using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillHive.Enums;

namespace SkillHive.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public CommentTargetType TargetType { get; set; }

        [Required]
        public int TargetId { get; set; }

        // Denormalized for scoping and fast lookups
        [Required]
        public int CourseId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        // null = top-level; set = reply (max 2 levels)
        public int? ParentCommentId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        [Required]
        public CommentStatus Status { get; set; } = CommentStatus.PUBLISHED;

        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        [ForeignKey(nameof(AuthorId))]
        public User Author { get; set; } = null!;

        [ForeignKey(nameof(ParentCommentId))]
        public Comment? ParentComment { get; set; }
    }
}