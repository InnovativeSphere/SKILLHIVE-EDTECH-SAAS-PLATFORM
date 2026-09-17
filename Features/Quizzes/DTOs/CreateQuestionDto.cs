using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class CreateQuestionDto
    {
        [Required]
        [StringLength(1000)]
        public string QuestionText { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Points { get; set; } = 1;

        public int? Order { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "A question must have at least 2 options")]
        public List<CreateOptionDto> Options { get; set; } = new();
    }
}