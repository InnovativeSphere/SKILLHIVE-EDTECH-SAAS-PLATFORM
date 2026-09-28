using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Models;

namespace SkillHive.Data.Seed
{
    public static class SeedPlans
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Only seed if no plans exist yet — idempotent across restarts
            if (await db.Plans.AnyAsync())
                return;

            var plans = new List<Plan>
            {
                new Plan
                {
                    Name = "Starter",
                    Slug = "starter",
                    Description = "For new academies finding their footing. Up to 5 courses, 3 staff members, and 100 students per course.",
                    Price = 5000m,
                    Currency = "NGN",
                    Interval = SubscriptionInterval.MONTHLY,
                    MaxCourses = 5,
                    MaxStaff = 3,
                    MaxStudentsPerCourse = 100,
                    CanChargeCourses = true,
                    CanUseCertificates = true,
                    CanUseCustomBranding = false,
                    IsActive = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Plan
                {
                    Name = "Pro",
                    Slug = "pro",
                    Description = "For growing academies. Up to 30 courses, 10 staff members, 500 students per course, and custom branding.",
                    Price = 15000m,
                    Currency = "NGN",
                    Interval = SubscriptionInterval.MONTHLY,
                    MaxCourses = 30,
                    MaxStaff = 10,
                    MaxStudentsPerCourse = 500,
                    CanChargeCourses = true,
                    CanUseCertificates = true,
                    CanUseCustomBranding = true,
                    IsActive = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Plan
                {
                    Name = "Enterprise",
                    Slug = "enterprise",
                    Description = "For established academies. Unlimited courses, staff, and students. All features included.",
                    Price = 50000m,
                    Currency = "NGN",
                    Interval = SubscriptionInterval.MONTHLY,
                    MaxCourses = null,
                    MaxStaff = null,
                    MaxStudentsPerCourse = null,
                    CanChargeCourses = true,
                    CanUseCertificates = true,
                    CanUseCustomBranding = true,
                    IsActive = true,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            db.Plans.AddRange(plans);
            await db.SaveChangesAsync();
        }
    }
}