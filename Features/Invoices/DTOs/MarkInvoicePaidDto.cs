using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Invoices.DTOs
{
    public class MarkInvoicePaidDto
    {
        // Optional external reference — e.g. bank transfer ref, cheque number,
        // or Paystack reference if paid outside the normal flow.
        [StringLength(100)]
        public string? Reference { get; set; }

        [Required]
        [StringLength(500)]
        [MinLength(5, ErrorMessage = "Please provide a reason of at least 5 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}