using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Enrollments.DTOs
{
    public class MarkLessonCompleteDto
    {
        [Required]
        public int LessonId { get; set; }
    }
}