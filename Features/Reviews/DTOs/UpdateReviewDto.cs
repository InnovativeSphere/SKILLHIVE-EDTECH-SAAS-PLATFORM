using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Reviews.DTOs
{
    public class UpdateReviewDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int? Rating { get; set; }

        [StringLength(150)]
        public string? Title { get; set; }

        [StringLength(2000)]
        [MinLength(10, ErrorMessage = "Review body must be at least 10 characters")]
        public string? Body { get; set; }
    }
}