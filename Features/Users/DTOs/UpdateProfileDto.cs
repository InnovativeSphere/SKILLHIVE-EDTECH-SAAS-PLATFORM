using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Users.DTOs
{
    public class UpdateProfileDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }
    }
}