using System.ComponentModel.DataAnnotations;
using SkillHive.Enums;

namespace SkillHive.Features.Courses.DTOs
{
    public class CreateCourseDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(5000)]
        public string? LongDescription { get; set; }

        [Required]
        public int ProfessionId { get; set; }

        public bool IsFree { get; set; } = true;

        [Range(0, 10000000)]
        public decimal? Price { get; set; }

        [Range(0, 10000000)]
        public decimal? DiscountPrice { get; set; }

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
    }
}