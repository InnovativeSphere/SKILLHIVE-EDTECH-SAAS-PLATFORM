using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Lessons.DTOs
{
    public class ReorderLessonDto
    {
        [Required]
        [Range(1, 1000)]
        public int NewOrder { get; set; }
    }
}