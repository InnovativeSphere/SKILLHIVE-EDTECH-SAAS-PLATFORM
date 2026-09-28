using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Subscriptions.DTOs
{
    public class AssignSubscriptionDto
    {
        [Required]
        public int AcademyId { get; set; }

        [Required]
        public int PlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        // Optional — if not provided, computed from plan.Interval using DateHelper
        public DateTime? EndDate { get; set; }

        public bool AutoRenew { get; set; } = true;

        // Optional grace period end — usually left null on fresh assignment
        public DateTime? GraceUntil { get; set; }
    }
}