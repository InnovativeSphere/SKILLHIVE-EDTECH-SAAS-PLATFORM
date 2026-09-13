using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Auth.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}