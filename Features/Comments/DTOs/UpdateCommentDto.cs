using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Comments.DTOs
{
    public class UpdateCommentDto
    {
        [Required]
        [StringLength(2000)]
        [MinLength(1)]
        public string Body { get; set; } = string.Empty;
    }
}