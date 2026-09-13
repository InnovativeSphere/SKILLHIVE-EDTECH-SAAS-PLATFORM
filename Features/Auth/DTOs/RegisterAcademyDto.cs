using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Auth.DTOs
{
    public class RegisterAcademyDto
    {
        [Required]
        [StringLength(150)]
        public string AcademyName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? AcademyDescription { get; set; }

        [Required]
        [StringLength(100)]
        public string OwnerFullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string OwnerEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string OwnerUsername { get; set; } = string.Empty;

        [StringLength(20)]
        public string? OwnerPhone { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}