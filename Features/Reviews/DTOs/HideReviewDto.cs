using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Reviews.DTOs
{
    public class HideReviewDto
    {
        [Required]
        [StringLength(500)]
        [MinLength(5)]
        public string Reason { get; set; } = string.Empty;
    }
}