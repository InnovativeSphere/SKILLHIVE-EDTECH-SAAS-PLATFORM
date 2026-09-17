using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }

        // Exactly one of CourseId or LessonId must be set
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int PassingScore { get; set; } = 70; // percentage

        public int? TimeLimitMinutes { get; set; }

        public int? MaxAttempts { get; set; } // null = unlimited

        public int? CooldownMinutes { get; set; } // null = no cooldown

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }

        public List<Question> Questions { get; set; } = new();
        public List<QuizAttempt> Attempts { get; set; } = new();
    }
}