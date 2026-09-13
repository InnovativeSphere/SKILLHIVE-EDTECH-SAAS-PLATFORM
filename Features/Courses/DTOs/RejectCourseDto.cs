using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Courses.DTOs
{
    public class RejectCourseDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}