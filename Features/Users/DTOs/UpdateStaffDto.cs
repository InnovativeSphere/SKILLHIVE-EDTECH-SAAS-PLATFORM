using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Users.DTOs
{
    public class UpdateStaffDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public string? Role { get; set; }
    }
}