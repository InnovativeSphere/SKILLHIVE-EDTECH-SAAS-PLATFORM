using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class Option
    {
        [Key]
        public int OptionId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [StringLength(500)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; } = false;

        [Required]
        public int Order { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;
    }
}