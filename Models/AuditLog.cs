using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }

        // Required — every action is performed by a user
        [Required]
        public int UserId { get; set; }

        // Nullable — auth actions, platform actions have no academy context
        public int? AcademyId { get; set; }

        // String constant, not enum. Examples:
        // LOGIN_SUCCESS, COURSE_PUBLISHED, CERTIFICATE_REVOKED, SUBSCRIPTION_CANCELLED
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        // "COURSE", "USER", "CERTIFICATE", "INVOICE", "SUBSCRIPTION", "PAYMENT", "ACADEMY", "STAFF"
        [StringLength(50)]
        public string? TargetType { get; set; }

        public int? TargetId { get; set; }

        // Optional rich context — before/after snapshots, reason strings.
        // Populated only for sensitive actions. Kept as text (not jsonb) —
        // we preserve the payload, we don't query inside it.
        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        // Captured automatically by AuditService from IHttpContextAccessor
        [StringLength(45)]
        public string? IpAddress { get; set; }

        // Append-only. Never updated, never deleted.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ─── Navigations ────────────────────────────────────
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(AcademyId))]
        public Academy? Academy { get; set; }
    }
}