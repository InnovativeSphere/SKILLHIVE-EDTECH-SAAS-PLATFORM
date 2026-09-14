using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Lessons.DTOs
{
    public class CreateLessonDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(10000)]
        public string? Content { get; set; }

        [StringLength(500)]
        public string? VideoUrl { get; set; }

        // Optional: if not provided, lesson is appended to end of course
        public int? Order { get; set; }

        public int? DurationMinutes { get; set; }

        public bool IsPreview { get; set; } = false;
    }
}