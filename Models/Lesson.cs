using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(10000)]
        public string? Content { get; set; }

        [StringLength(500)]
        public string? VideoUrl { get; set; }

        [Required]
        public int Order { get; set; }

        public int? DurationMinutes { get; set; }

        public bool IsPreview { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;
    }
}