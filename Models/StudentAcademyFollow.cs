using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class StudentAcademyFollow
    {
        [Key]
        public int FollowId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int AcademyId { get; set; }

        [Required]
        public bool NotifyOnNewCourse { get; set; } = true;

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(StudentId))]
        public User Student { get; set; } = null!;

        [ForeignKey(nameof(AcademyId))]
        public Academy Academy { get; set; } = null!;
    }
}