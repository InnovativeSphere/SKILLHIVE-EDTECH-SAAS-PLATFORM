using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Models;

namespace SkillHive.Data.Seed
{
    public static class SeedUsers
    {
        public static async Task SeedSuperAdminAsync(AppDbContext db)
        {
            const string email = "superadmin@skillhive.com";

            if (await db.Users.AnyAsync(u => u.Email == email))
                return;

            var superAdmin = new User
            {
                FullName = "Platform Super Admin",
                Username = "superadmin",
                Email = email,
                Phone = null,
                PasswordHash = Utils.HashPassword("SuperAdmin@123"),
                Role = UserRole.SUPER_ADMIN,
                AcademyId = null,
                Status = UserStatus.ACTIVE,
                EmailVerified = true,
                InvitedByUserId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(superAdmin);
            await db.SaveChangesAsync();
        }
    }
}