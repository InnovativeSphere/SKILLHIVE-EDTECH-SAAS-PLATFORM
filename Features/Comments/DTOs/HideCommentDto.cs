using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Comments.DTOs
{
    public class HideCommentDto
    {
        [Required]
        [StringLength(500)]
        [MinLength(5)]
        public string Reason { get; set; } = string.Empty;
    }
}