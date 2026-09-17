using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class UpdateQuizDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public int? PassingScore { get; set; }

        [Range(1, 600)]
        public int? TimeLimitMinutes { get; set; }

        [Range(1, 100)]
        public int? MaxAttempts { get; set; }

        [Range(0, 10080)]
        public int? CooldownMinutes { get; set; }
    }
}