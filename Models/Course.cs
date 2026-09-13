using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(5000)]
        public string? LongDescription { get; set; }

        [Required]
        public int AcademyId { get; set; }

        [Required]
        public int InstructorId { get; set; }

        [Required]
        public int ProfessionId { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public bool IsFree { get; set; } = true;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountPrice { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "NGN";

        [StringLength(500)]
        public string? CoverImageUrl { get; set; }

        [StringLength(500)]
        public string? PromoVideoUrl { get; set; }

        public CourseLevel Level { get; set; } = CourseLevel.BEGINNER;

        [StringLength(50)]
        public string Language { get; set; } = "English";

        public int? EstimatedDurationMinutes { get; set; }

        [StringLength(1000)]
        public string? Prerequisites { get; set; }

        public CourseStatus Status { get; set; } = CourseStatus.DRAFT;

        public CourseVisibility Visibility { get; set; } = CourseVisibility.PUBLIC;

        public DateTime? PublishedAt { get; set; }

        public DateTime? ArchivedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public int TotalLessons { get; set; } = 0;

        public int TotalEnrollments { get; set; } = 0;

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AverageRating { get; set; }

        public int TotalReviews { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AcademyId))]
        public Academy Academy { get; set; } = null!;

        [ForeignKey(nameof(InstructorId))]
        public User Instructor { get; set; } = null!;

        [ForeignKey(nameof(ProfessionId))]
        public Profession Profession { get; set; } = null!;

        [ForeignKey(nameof(CreatedByUserId))]
        public User CreatedBy { get; set; } = null!;
    }
}