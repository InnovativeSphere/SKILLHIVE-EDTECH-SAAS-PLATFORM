using System.ComponentModel.DataAnnotations;
using SkillHive.Enums;

namespace SkillHive.Features.Comments.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public CommentTargetType TargetType { get; set; }

        [Required]
        public int TargetId { get; set; }

        [Required]
        [StringLength(2000)]
        [MinLength(1)]
        public string Body { get; set; } = string.Empty;

        // If set, this is a reply to another comment (max 2 levels)
        public int? ParentCommentId { get; set; }
    }
}