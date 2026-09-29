using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        // Human-readable, unique, per-year sequential
        // Format: INV-2026-00042
        [Required]
        [StringLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        // Owning subscription — the source of the billing relationship
        [Required]
        public int SubscriptionId { get; set; }

        // Denormalized from Subscription for fast scoping and reporting
        [Required]
        public int AcademyId { get; set; }

        // Denormalized snapshot of the plan at billing time.
        // If a plan is renamed or repriced later, the invoice still reflects
        // what the customer was billed under.
        [Required]
        public int PlanId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountDue { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.DRAFT;

        // When the invoice was created and sent to the customer
        [Required]
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        // When payment is expected by
        [Required]
        public DateTime DueDate { get; set; }

        // Populated when payment succeeds
        public DateTime? PaidAt { get; set; }

        // Populated on void — invoice is preserved, not deleted
        public DateTime? VoidedAt { get; set; }

        [StringLength(500)]
        public string? VoidedReason { get; set; }

        // The billing period this invoice covers — distinct from IssuedAt.
        // Example: issued Oct 28, covers Nov 1 – Nov 30.
        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        [ForeignKey(nameof(SubscriptionId))]
        public Subscription Subscription { get; set; } = null!;

        [ForeignKey(nameof(AcademyId))]
        public Academy Academy { get; set; } = null!;

        [ForeignKey(nameof(PlanId))]
        public Plan Plan { get; set; } = null!;
    }
}