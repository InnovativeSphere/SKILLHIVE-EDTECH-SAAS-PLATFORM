using System.ComponentModel.DataAnnotations;
using SkillHive.Enums;

namespace SkillHive.Features.Subscriptions.DTOs
{
    public class CreatePlanDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Optional — auto-generated from Name if not provided
        [StringLength(100)]
        public string? Slug { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 10_000_000, ErrorMessage = "Price must be between 0 and 10,000,000")]
        public decimal Price { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        public SubscriptionInterval Interval { get; set; } = SubscriptionInterval.MONTHLY;

        // Null = unlimited
        [Range(1, 10_000)]
        public int? MaxCourses { get; set; }

        [Range(1, 1_000)]
        public int? MaxStaff { get; set; }

        [Range(1, 1_000_000)]
        public int? MaxStudentsPerCourse { get; set; }

        // Feature flags — defaults match Plan model
        public bool CanChargeCourses { get; set; } = true;
        public bool CanUseCertificates { get; set; } = true;
        public bool CanUseCustomBranding { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;
    }
}