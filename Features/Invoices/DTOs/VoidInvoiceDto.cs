using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Invoices.DTOs
{
    public class VoidInvoiceDto
    {
        [Required]
        [StringLength(500)]
        [MinLength(5, ErrorMessage = "Please provide a reason of at least 5 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}