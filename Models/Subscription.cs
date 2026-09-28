using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        // One active subscription per academy (unique index enforced in DbContext)
        [Required]
        public int AcademyId { get; set; }

        [Required]
        public int PlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.TRIAL;

        public bool AutoRenew { get; set; } = true;

        // Grace period ends here when status moves to GRACE
        public DateTime? GraceUntil { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        [ForeignKey(nameof(AcademyId))]
        public Academy Academy { get; set; } = null!;

        [ForeignKey(nameof(PlanId))]
        public Plan Plan { get; set; } = null!;
    }
}