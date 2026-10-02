using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Payments.DTOs
{
    public class InitializeSubscriptionPaymentDto
    {
        [Required]
        public int InvoiceId { get; set; }
    }
}