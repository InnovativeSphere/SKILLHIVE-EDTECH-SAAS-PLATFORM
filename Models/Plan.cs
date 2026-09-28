using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Plan
    {
        [Key]
        public int PlanId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        public SubscriptionInterval Interval { get; set; } = SubscriptionInterval.MONTHLY;

        // Plan limits — null means unlimited
        public int? MaxCourses { get; set; }
        public int? MaxStaff { get; set; }
        public int? MaxStudentsPerCourse { get; set; }

        // Feature flags
        public bool CanChargeCourses { get; set; } = true;
        public bool CanUseCertificates { get; set; } = true;
        public bool CanUseCustomBranding { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        public List<Subscription> Subscriptions { get; set; } = new();
    }
}