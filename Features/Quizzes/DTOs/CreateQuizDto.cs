using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class CreateQuizDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // Exactly one of CourseId or LessonId must be set
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }

        [Range(0, 100)]
        public int PassingScore { get; set; } = 70;

        [Range(1, 600)]
        public int? TimeLimitMinutes { get; set; }

        [Range(1, 100)]
        public int? MaxAttempts { get; set; }

        [Range(0, 10080)] // up to 1 week
        public int? CooldownMinutes { get; set; }
    }
}