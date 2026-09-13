using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Notifications.Services;
using SkillHive.Features.Users.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Users.Services
{
    public class StaffService
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;
        private readonly IConfiguration _config;

        public StaffService(AppDbContext db, NotificationService notifications, IConfiguration config)
        {
            _db = db;
            _notifications = notifications;
            _config = config;
        }

        // ─── Invite a Staff Member (Owner only) ────────────────────
        public async Task<object> InviteStaffAsync(int ownerUserId, InviteStaffDto dto)
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.UserId == ownerUserId);
            if (owner == null || owner.Role != UserRole.ACADEMY_OWNER)
                throw new UnauthorizedAccessException("Only academy owners can invite staff");

            if (owner.AcademyId == null)
                throw new InvalidOperationException("You do not have an academy");

            if (!Enum.TryParse<UserRole>(dto.Role.ToUpperInvariant(), out var role))
                throw new InvalidOperationException("Invalid role");

            if (role != UserRole.INSTRUCTOR && role != UserRole.MODERATOR)
                throw new InvalidOperationException("You can only invite INSTRUCTOR or MODERATOR");

            var email = dto.Email.ToLowerInvariant().Trim();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                throw new InvalidOperationException("Email is already in use");

            var username = await GenerateUniqueUsernameAsync(email);
            var unusablePasswordHash = Utils.HashPassword(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

            var staff = new User
            {
                FullName = Utils.ToTitleCase(Utils.SanitizeInput(dto.FullName)),
                Username = username,
                Email = email,
                Phone = dto.Phone,
                PasswordHash = unusablePasswordHash,
                Role = role,
                AcademyId = owner.AcademyId.Value,
                Status = UserStatus.INVITED,
                EmailVerified = false,
                InvitedByUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(staff);
            await _db.SaveChangesAsync();

            var inviteToken = TokenHelper.GenerateVerificationToken();
            _db.VerificationTokens.Add(new VerificationToken
            {
                UserId = staff.UserId,
                Token = inviteToken,
                Type = VerificationTokenType.INVITE,
                ExpiresAt = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var platformName = _config["App:Name"] ?? "SkillHive";
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5075";
            var acceptLink = $"{baseUrl}/accept-invite?token={inviteToken}";

            await _notifications.NotifyAsync(
                staff.UserId,
                NotificationType.STAFF_INVITE,
                "You've been invited",
                $"{owner.FullName} has invited you to join their academy on {platformName}.",
                sendEmail: true,
                emailTemplate: "staff-invite",
                emailModel: new
                {
                    FullName = staff.FullName,
                    InviterName = owner.FullName,
                    Role = role.ToString(),
                    AcceptLink = acceptLink,
                    PlatformName = platformName,
                    ExpiryHours = 72
                });

            return new
            {
                userId = staff.UserId,
                fullName = staff.FullName,
                email = staff.Email,
                username = staff.Username,
                role = staff.Role.ToString(),
                status = staff.Status.ToString(),
                message = "Invitation sent"
            };
        }

        // ─── List All Staff (Owner / Instructor / Moderator) ──────
        public async Task<List<object>> GetStaffListAsync(int requesterUserId)
        {
            var requester = await _db.Users.FirstOrDefaultAsync(u => u.UserId == requesterUserId);
            if (requester?.AcademyId == null)
                throw new InvalidOperationException("You do not belong to an academy");

            var staff = await _db.Users
                .Where(u => u.AcademyId == requester.AcademyId
                            && (u.Role == UserRole.ACADEMY_OWNER
                                || u.Role == UserRole.INSTRUCTOR
                                || u.Role == UserRole.MODERATOR))
                .OrderBy(u => u.CreatedAt)
                .Select(u => new
                {
                    userId = u.UserId,
                    fullName = u.FullName,
                    username = u.Username,
                    email = u.Email,
                    phone = u.Phone,
                    role = u.Role.ToString(),
                    status = u.Status.ToString(),
                    emailVerified = u.EmailVerified,
                    invitedByUserId = u.InvitedByUserId,
                    lastLogin = u.LastLogin,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();

            return staff.Cast<object>().ToList();
        }

        // ─── Update Staff (Owner only) ─────────────────────────────
        public async Task<object> UpdateStaffAsync(int ownerUserId, int staffId, UpdateStaffDto dto)
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.UserId == ownerUserId);
            if (owner == null || owner.Role != UserRole.ACADEMY_OWNER || owner.AcademyId == null)
                throw new UnauthorizedAccessException("Only academy owners can update staff");

            var staff = await _db.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.AcademyId == owner.AcademyId);
            if (staff == null)
                throw new InvalidOperationException("Staff member not found in your academy");

            if (staff.Role == UserRole.ACADEMY_OWNER)
                throw new InvalidOperationException("Cannot modify the academy owner");

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                staff.FullName = Utils.ToTitleCase(Utils.SanitizeInput(dto.FullName));

            if (dto.Phone != null)
                staff.Phone = dto.Phone;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (!Enum.TryParse<UserRole>(dto.Role.ToUpperInvariant(), out var newRole))
                    throw new InvalidOperationException("Invalid role");

                if (newRole != UserRole.INSTRUCTOR && newRole != UserRole.MODERATOR)
                    throw new InvalidOperationException("Role must be INSTRUCTOR or MODERATOR");

                staff.Role = newRole;
            }

            staff.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                userId = staff.UserId,
                fullName = staff.FullName,
                email = staff.Email,
                username = staff.Username,
                phone = staff.Phone,
                role = staff.Role.ToString(),
                status = staff.Status.ToString(),
                updatedAt = staff.UpdatedAt
            };
        }

        // ─── Deactivate Staff (Owner only) ─────────────────────────
        public async Task<object> DeactivateStaffAsync(int ownerUserId, int staffId)
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.UserId == ownerUserId);
            if (owner == null || owner.Role != UserRole.ACADEMY_OWNER || owner.AcademyId == null)
                throw new UnauthorizedAccessException("Only academy owners can deactivate staff");

            if (staffId == ownerUserId)
                throw new InvalidOperationException("You cannot deactivate yourself");

            var staff = await _db.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.AcademyId == owner.AcademyId);
            if (staff == null)
                throw new InvalidOperationException("Staff member not found in your academy");

            if (staff.Role == UserRole.ACADEMY_OWNER)
                throw new InvalidOperationException("Cannot deactivate the academy owner");

            staff.Status = UserStatus.INACTIVE;
            staff.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { userId = staff.UserId, status = staff.Status.ToString(), message = "Staff deactivated" };
        }

        // ─── Reactivate Staff (Owner only) ─────────────────────────
        public async Task<object> ReactivateStaffAsync(int ownerUserId, int staffId)
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.UserId == ownerUserId);
            if (owner == null || owner.Role != UserRole.ACADEMY_OWNER || owner.AcademyId == null)
                throw new UnauthorizedAccessException("Only academy owners can reactivate staff");

            var staff = await _db.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.AcademyId == owner.AcademyId);
            if (staff == null)
                throw new InvalidOperationException("Staff member not found in your academy");

            staff.Status = UserStatus.ACTIVE;
            staff.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { userId = staff.UserId, status = staff.Status.ToString(), message = "Staff reactivated" };
        }

        // ─── Helpers ───────────────────────────────────────────────
        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            var baseName = email.Split('@')[0].ToLowerInvariant()
                .Replace(".", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);

            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "user";

            var candidate = baseName;
            var suffix = 1;

            while (await _db.Users.AnyAsync(u => u.Username == candidate))
            {
                candidate = $"{baseName}{suffix}";
                suffix++;
            }

            return candidate;
        }
    }
}