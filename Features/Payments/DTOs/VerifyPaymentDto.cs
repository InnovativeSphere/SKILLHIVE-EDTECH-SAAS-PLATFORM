using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Payments.DTOs
{
    public class VerifyPaymentDto
    {
        [Required]
        [StringLength(100)]
        public string Reference { get; set; } = string.Empty;
    }
}