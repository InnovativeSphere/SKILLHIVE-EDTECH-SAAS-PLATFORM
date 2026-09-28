using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Comments.DTOs
{
    public class PinCommentDto
    {
        [Required]
        public bool IsPinned { get; set; }
    }
}