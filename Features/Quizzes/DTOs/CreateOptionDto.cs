using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class CreateOptionDto
    {
        [Required]
        [StringLength(500)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        public int? Order { get; set; }
    }
}