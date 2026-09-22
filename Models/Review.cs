using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int StudentId { get; set; }

        // Uniqueness anchor — one review per enrollment
        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(150)]
        public string? Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        public bool IsVerifiedPurchase { get; set; } = false;

        [Required]
        public ReviewStatus Status { get; set; } = ReviewStatus.PUBLISHED;

        // Denormalized helpful counter
        public int HelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        [ForeignKey(nameof(StudentId))]
        public User Student { get; set; } = null!;

        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;
    }
}