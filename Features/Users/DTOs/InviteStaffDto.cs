using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Users.DTOs
{
    public class InviteStaffDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}