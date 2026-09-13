using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Auth.DTOs;
using SkillHive.Features.Notifications.Services;
using SkillHive.Models;

namespace SkillHive.Features.Auth.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly JwtHelper _jwt;
        private readonly NotificationService _notifications;
        private readonly IConfiguration _config;

        public AuthService(
            AppDbContext db,
            JwtHelper jwt,
            NotificationService notifications,
            IConfiguration config)
        {
            _db = db;
            _jwt = jwt;
            _notifications = notifications;
            _config = config;
        }

        // ─── Register Academy Owner ────────────────────────────────
        public async Task<object> RegisterAcademyAsync(RegisterAcademyDto dto)
        {
            await EnsureEmailAndUsernameFreeAsync(dto.OwnerEmail, dto.OwnerUsername);

            var passwordHash = Utils.HashPassword(dto.Password);
            var baseSlug = SlugHelper.GenerateSlug(dto.AcademyName);
            var slug = await SlugHelper.EnsureUniqueSlugAsync(
                baseSlug,
                async s => await _db.Academies.AnyAsync(a => a.Slug == s));

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var owner = new User
                {
                    FullName = Utils.ToTitleCase(Utils.SanitizeInput(dto.OwnerFullName)),
                    Username = dto.OwnerUsername.ToLowerInvariant().Trim(),
                    Email = dto.OwnerEmail.ToLowerInvariant().Trim(),
                    Phone = dto.OwnerPhone,
                    PasswordHash = passwordHash,
                    Role = UserRole.ACADEMY_OWNER,
                    AcademyId = null,
                    Status = UserStatus.ACTIVE,
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Users.Add(owner);
                await _db.SaveChangesAsync();

                var academy = new Academy
                {
                    Name = Utils.ToTitleCase(Utils.SanitizeInput(dto.AcademyName)),
                    Slug = slug,
                    Description = dto.AcademyDescription,
                    Email = owner.Email,
                    OwnerId = owner.UserId,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Academies.Add(academy);
                await _db.SaveChangesAsync();

                owner.AcademyId = academy.AcademyId;
                await _db.SaveChangesAsync();

                // TODO: Create trial subscription here once Subscriptions module is built.

                var otp = TokenHelper.GenerateOtp();
                var verificationToken = new VerificationToken
                {
                    UserId = owner.UserId,
                    Token = otp,
                    Type = VerificationTokenType.OTP,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Used = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.VerificationTokens.Add(verificationToken);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                var platformName = _config["App:Name"] ?? "SkillHive";

                await _notifications.NotifyAsync(
                    owner.UserId,
                    NotificationType.OTP_VERIFICATION,
                    "Verify your email",
                    "Use the code we just sent to verify your account.",
                    sendEmail: true,
                    emailTemplate: "otp-verification",
                    emailModel: new
                    {
                        FullName = owner.FullName,
                        Otp = otp,
                        PlatformName = platformName,
                        ExpiryMinutes = 10
                    });

                return new
                {
                    userId = owner.UserId,
                    academyId = academy.AcademyId,
                    fullName = owner.FullName,
                    email = owner.Email,
                    username = owner.Username,
                    role = owner.Role.ToString(),
                    emailVerified = false,
                    message = "Academy registered. Please check your email for the OTP."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─── Register Student ──────────────────────────────────────
        public async Task<object> RegisterStudentAsync(RegisterStudentDto dto)
        {
            await EnsureEmailAndUsernameFreeAsync(dto.Email, dto.Username);

            var student = new User
            {
                FullName = Utils.ToTitleCase(Utils.SanitizeInput(dto.FullName)),
                Username = dto.Username.ToLowerInvariant().Trim(),
                Email = dto.Email.ToLowerInvariant().Trim(),
                Phone = dto.Phone,
                PasswordHash = Utils.HashPassword(dto.Password),
                Role = UserRole.STUDENT,
                AcademyId = null,
                Status = UserStatus.ACTIVE,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(student);
            await _db.SaveChangesAsync();

            var token = TokenHelper.GenerateVerificationToken();
            var verificationToken = new VerificationToken
            {
                UserId = student.UserId,
                Token = token,
                Type = VerificationTokenType.EMAIL_VERIFICATION,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Used = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.VerificationTokens.Add(verificationToken);
            await _db.SaveChangesAsync();

            var platformName = _config["App:Name"] ?? "SkillHive";
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5075";
            var verifyLink = $"{baseUrl}/api/auth/verify-email?token={token}";

            await _notifications.NotifyAsync(
                student.UserId,
                NotificationType.EMAIL_VERIFICATION,
                "Verify your email",
                "Click the link we sent to verify your email and unlock enrollment.",
                sendEmail: true,
                emailTemplate: "email-verification",
                emailModel: new
                {
                    FullName = student.FullName,
                    VerifyLink = verifyLink,
                    PlatformName = platformName,
                    ExpiryHours = 24
                });

            return new
            {
                userId = student.UserId,
                fullName = student.FullName,
                email = student.Email,
                username = student.Username,
                role = student.Role.ToString(),
                emailVerified = false,
                message = "Registration successful. Please verify your email."
            };
        }

        // ─── Login ─────────────────────────────────────────────────
        public async Task<object> LoginAsync(LoginDto dto)
        {
            var identifier = dto.UsernameOrEmail.ToLowerInvariant().Trim();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);

            if (user == null || !Utils.VerifyPassword(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            if (user.Status == UserStatus.INACTIVE)
                throw new UnauthorizedAccessException("Your account has been deactivated");

            if (user.Status == UserStatus.LOCKED)
                throw new UnauthorizedAccessException("Your account is locked");

            if (user.Status == UserStatus.SUSPENDED)
                throw new UnauthorizedAccessException("Your account is suspended");

            if (user.Status == UserStatus.INVITED)
                throw new UnauthorizedAccessException("Please set your password using the invite link sent to your email");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(
                user.UserId,
                user.Email,
                user.Role.ToString(),
                user.AcademyId);

            return new
            {
                token,
                user = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    username = user.Username,
                    role = user.Role.ToString(),
                    academyId = user.AcademyId,
                    emailVerified = user.EmailVerified
                }
            };
        }

        // ─── Verify OTP (Academy Owner) ────────────────────────────
        public async Task<object> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var email = dto.Email.ToLowerInvariant().Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.EmailVerified)
                return new { message = "Email already verified" };

            var token = await _db.VerificationTokens
                .Where(v => v.UserId == user.UserId
                            && v.Type == VerificationTokenType.OTP
                            && !v.Used)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
                throw new InvalidOperationException("No active OTP found. Please request a new one.");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("OTP has expired. Please request a new one.");

            if (token.Token != dto.Otp.Trim())
                throw new InvalidOperationException("Invalid OTP");

            token.Used = true;
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _notifications.NotifyAsync(
                user.UserId,
                NotificationType.WELCOME,
                "Welcome to SkillHive",
                "Your account is verified. You can now start managing your academy.",
                sendEmail: false);

            return new
            {
                userId = user.UserId,
                emailVerified = true,
                message = "Email verified successfully"
            };
        }

        // ─── Verify Email Link (Student) ───────────────────────────
        public async Task<object> VerifyEmailAsync(VerifyEmailDto dto)
        {
            var token = await _db.VerificationTokens
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == dto.Token
                                          && v.Type == VerificationTokenType.EMAIL_VERIFICATION
                                          && !v.Used);

            if (token == null)
                throw new InvalidOperationException("Invalid or already used token");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Verification link has expired");

            token.Used = true;
            token.User.EmailVerified = true;
            token.User.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _notifications.NotifyAsync(
                token.User.UserId,
                NotificationType.WELCOME,
                "Welcome to SkillHive",
                "Your email is verified. You can now enroll in courses.",
                sendEmail: false);

            return new
            {
                userId = token.User.UserId,
                emailVerified = true,
                message = "Email verified successfully"
            };
        }

        // ─── Resend Verification ───────────────────────────────────
        public async Task<object> ResendVerificationAsync(ResendVerificationDto dto)
        {
            var email = dto.Email.ToLowerInvariant().Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return new { message = "If this email exists, a new verification message has been sent." };

            if (user.EmailVerified)
                return new { message = "This email is already verified." };

            // Invalidate prior unused tokens of the same type
            var oldTokens = await _db.VerificationTokens
                .Where(v => v.UserId == user.UserId && !v.Used
                            && (v.Type == VerificationTokenType.OTP
                                || v.Type == VerificationTokenType.EMAIL_VERIFICATION))
                .ToListAsync();

            foreach (var old in oldTokens)
                old.Used = true;

            var platformName = _config["App:Name"] ?? "SkillHive";
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5075";

            if (user.Role == UserRole.ACADEMY_OWNER)
            {
                var otp = TokenHelper.GenerateOtp();
                _db.VerificationTokens.Add(new VerificationToken
                {
                    UserId = user.UserId,
                    Token = otp,
                    Type = VerificationTokenType.OTP,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                await _notifications.NotifyAsync(
                    user.UserId,
                    NotificationType.OTP_VERIFICATION,
                    "Your new verification code",
                    "Use the code we just sent to verify your account.",
                    sendEmail: true,
                    emailTemplate: "otp-verification",
                    emailModel: new
                    {
                        FullName = user.FullName,
                        Otp = otp,
                        PlatformName = platformName,
                        ExpiryMinutes = 10
                    });
            }
            else
            {
                var token = TokenHelper.GenerateVerificationToken();
                _db.VerificationTokens.Add(new VerificationToken
                {
                    UserId = user.UserId,
                    Token = token,
                    Type = VerificationTokenType.EMAIL_VERIFICATION,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                var verifyLink = $"{baseUrl}/api/auth/verify-email?token={token}";

                await _notifications.NotifyAsync(
                    user.UserId,
                    NotificationType.EMAIL_VERIFICATION,
                    "Verify your email",
                    "Click the link we sent to verify your email.",
                    sendEmail: true,
                    emailTemplate: "email-verification",
                    emailModel: new
                    {
                        FullName = user.FullName,
                        VerifyLink = verifyLink,
                        PlatformName = platformName,
                        ExpiryHours = 24
                    });
            }

            return new { message = "Verification message sent. Please check your email." };
        }

        // ─── Forgot Password ───────────────────────────────────────
        public async Task<object> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var email = dto.Email.ToLowerInvariant().Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Always return the same message — never reveal whether the email exists
            if (user == null)
                return new { message = "If an account with that email exists, a reset link has been sent." };

            // Invalidate prior unused reset tokens
            var oldTokens = await _db.VerificationTokens
                .Where(v => v.UserId == user.UserId && !v.Used
                            && v.Type == VerificationTokenType.PASSWORD_RESET)
                .ToListAsync();

            foreach (var old in oldTokens)
                old.Used = true;

            var token = TokenHelper.GenerateVerificationToken();
            _db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.UserId,
                Token = token,
                Type = VerificationTokenType.PASSWORD_RESET,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var platformName = _config["App:Name"] ?? "SkillHive";
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5075";
            var resetLink = $"{baseUrl}/reset-password?token={token}";

            await _notifications.NotifyAsync(
                user.UserId,
                NotificationType.PASSWORD_RESET,
                "Reset your password",
                "Click the link to reset your password. This link expires in 1 hour.",
                sendEmail: true,
                emailTemplate: "password-reset",
                emailModel: new
                {
                    FullName = user.FullName,
                    ResetLink = resetLink,
                    PlatformName = platformName,
                    ExpiryMinutes = 60
                });

            return new { message = "If an account with that email exists, a reset link has been sent." };
        }

        // ─── Reset Password ────────────────────────────────────────
        public async Task<object> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var token = await _db.VerificationTokens
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == dto.Token
                                          && v.Type == VerificationTokenType.PASSWORD_RESET
                                          && !v.Used);

            if (token == null)
                throw new InvalidOperationException("Invalid or already used reset token");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Reset link has expired");

            token.Used = true;
            token.User.PasswordHash = Utils.HashPassword(dto.NewPassword);
            token.User.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { message = "Password reset successful. You can now log in." };
        }

                // ─── Accept Invite (Staff) ─────────────────────────────────
        public async Task<object> AcceptInviteAsync(AcceptInviteDto dto)
        {
            var token = await _db.VerificationTokens
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == dto.Token
                                          && v.Type == VerificationTokenType.INVITE
                                          && !v.Used);

            if (token == null)
                throw new InvalidOperationException("Invalid or already used invite token");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Invite link has expired");

            var user = token.User;

            if (user.Status != UserStatus.INVITED)
                throw new InvalidOperationException("This invite has already been accepted");

            token.Used = true;
            user.PasswordHash = Utils.HashPassword(dto.Password);
            user.EmailVerified = true;
            user.Status = UserStatus.ACTIVE;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var jwt = _jwt.GenerateToken(
                user.UserId,
                user.Email,
                user.Role.ToString(),
                user.AcademyId);

            await _notifications.NotifyAsync(
                user.UserId,
                NotificationType.WELCOME,
                "Welcome to SkillHive",
                "Your account is now active. You can start using your academy workspace.",
                sendEmail: false);

            return new
            {
                token = jwt,
                user = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    username = user.Username,
                    role = user.Role.ToString(),
                    academyId = user.AcademyId,
                    emailVerified = true
                }
            };
        }

        // ─── Helpers ───────────────────────────────────────────────
        private async Task EnsureEmailAndUsernameFreeAsync(string email, string username)
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();
            var normalizedUsername = username.ToLowerInvariant().Trim();

            if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
                throw new InvalidOperationException("Email is already in use");

            if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername))
                throw new InvalidOperationException("Username is already taken");
        }
    }


}