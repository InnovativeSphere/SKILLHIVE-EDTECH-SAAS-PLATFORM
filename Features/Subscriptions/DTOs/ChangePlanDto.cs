using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Subscriptions.DTOs
{
    public class ChangePlanDto
    {
        [Required]
        public int NewPlanId { get; set; }

        // Optional reason for audit trail
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}