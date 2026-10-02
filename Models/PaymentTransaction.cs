using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int PaymentId { get; set; }

        // Unique Paystack reference — format SKH-{timestamp}-{random}
        // The unique index in DbContext is what makes webhook idempotency possible.
        [Required]
        [StringLength(100)]
        public string Reference { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        [Required]
        public PaymentProvider Provider { get; set; } = PaymentProvider.PAYSTACK;

        // Paystack channel — "card", "bank", "ussd", "transfer", "mobile_money"
        // Null until webhook confirms.
        [StringLength(30)]
        public string? Channel { get; set; }

        // Populated when Paystack confirms success
        public DateTime? PaidAt { get; set; }

        [Required]
        public PaymentPurpose Purpose { get; set; }

        // ─── Subscription flow ─────────────────────────────
        // Set for SUBSCRIPTION payments. AcademyId is the payer;
        // InvoiceId points to the invoice being settled.
        public int? AcademyId { get; set; }
        public int? InvoiceId { get; set; }

        // ─── Course purchase flow ──────────────────────────
        // Set for COURSE_PURCHASE payments. StudentId is the payer;
        // CourseId is the course being purchased.
        public int? StudentId { get; set; }
        public int? CourseId { get; set; }

        // Raw Paystack payload (webhook or verify response) as JSON.
        // Preserved for audit — helps diagnose disputes and reconciliations.
        [Column(TypeName = "text")]
        public string? ProviderPayload { get; set; }

        // Optional error note when status = FAILED
        [StringLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ─── Navigations (all optional, depends on Purpose) ─

        [ForeignKey(nameof(AcademyId))]
        public Academy? Academy { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }
    }
}