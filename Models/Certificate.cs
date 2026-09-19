using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        // Denormalized for fast lookup — every certificate query needs these
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int AcademyId { get; set; }

        [Required]
        [StringLength(50)]
        public string VerificationCode { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string PdfUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CloudinaryPublicId { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        // null = auto-generated on enrollment completion; set = manual reissue by a user
        public int? IssuedByUserId { get; set; }

        [Required]
        public CertificateStatus Status { get; set; } = CertificateStatus.ISSUED;

        public DateTime? RevokedAt { get; set; }

        [StringLength(500)]
        public string? RevokedReason { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; } = null!;

        [ForeignKey(nameof(StudentId))]
        public User Student { get; set; } = null!;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        [ForeignKey(nameof(AcademyId))]
        public Academy Academy { get; set; } = null!;

        [ForeignKey(nameof(IssuedByUserId))]
        public User? IssuedBy { get; set; }
    }
}