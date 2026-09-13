using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Auth.DTOs
{
    public class LoginDto
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}