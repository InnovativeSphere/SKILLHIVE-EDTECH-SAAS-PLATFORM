using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Auth.DTOs
{
    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}