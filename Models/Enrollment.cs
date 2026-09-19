using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        // Set only for paid courses. Null for free.
        public int? PaymentId { get; set; }

        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ACTIVE;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? CertificateIssuedAt { get; set; }

        [Required]
        public int ProgressPercentage { get; set; } = 0;

        [ForeignKey(nameof(StudentId))]
        public User Student { get; set; } = null!;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        public List<LessonProgress> LessonProgresses { get; set; } = new();
    }
}