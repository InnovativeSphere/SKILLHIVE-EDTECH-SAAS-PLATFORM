using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Features.Users.DTOs;

namespace SkillHive.Features.Users.Services
{
    public class UserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<object> GetProfileAsync(int userId)
        {
            var user = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    userId = u.UserId,
                    fullName = u.FullName,
                    username = u.Username,
                    email = u.Email,
                    phone = u.Phone,
                    role = u.Role.ToString(),
                    academyId = u.AcademyId,
                    status = u.Status.ToString(),
                    emailVerified = u.EmailVerified,
                    lastLogin = u.LastLogin,
                    createdAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                throw new InvalidOperationException("User not found");

            return user;
        }

        public async Task<object> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = Utils.ToTitleCase(Utils.SanitizeInput(dto.FullName));

            if (dto.Phone != null)
                user.Phone = dto.Phone;

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                userId = user.UserId,
                fullName = user.FullName,
                username = user.Username,
                email = user.Email,
                phone = user.Phone,
                updatedAt = user.UpdatedAt
            };
        }

        public async Task<object> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            if (!Utils.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Current password is incorrect");

            user.PasswordHash = Utils.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { message = "Password changed successfully" };
        }
    }
}