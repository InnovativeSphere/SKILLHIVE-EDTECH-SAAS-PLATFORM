using System.ComponentModel.DataAnnotations;
using SkillHive.Enums;

namespace SkillHive.Features.Subscriptions.DTOs
{
    public class UpdatePlanDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Price change is allowed but service will warn if active subscriptions exist
        [Range(0, 10_000_000, ErrorMessage = "Price must be between 0 and 10,000,000")]
        public decimal? Price { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; }

        public SubscriptionInterval? Interval { get; set; }

        [Range(1, 10_000)]
        public int? MaxCourses { get; set; }

        [Range(1, 1_000)]
        public int? MaxStaff { get; set; }

        [Range(1, 1_000_000)]
        public int? MaxStudentsPerCourse { get; set; }

        public bool? CanChargeCourses { get; set; }
        public bool? CanUseCertificates { get; set; }
        public bool? CanUseCustomBranding { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsPublic { get; set; }
    }
}