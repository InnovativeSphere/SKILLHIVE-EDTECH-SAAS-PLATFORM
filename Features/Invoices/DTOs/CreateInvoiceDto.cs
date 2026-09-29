using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Invoices.DTOs
{
    public class CreateInvoiceDto
    {
        [Required]
        public int SubscriptionId { get; set; }

        [Required]
        [Range(0, 100_000_000, ErrorMessage = "Amount must be between 0 and 100,000,000")]
        public decimal AmountDue { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public DateTime DueDate { get; set; }
    }
}