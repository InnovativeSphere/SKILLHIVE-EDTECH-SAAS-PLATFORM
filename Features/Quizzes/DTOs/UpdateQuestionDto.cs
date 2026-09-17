using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class UpdateQuestionDto
    {
        [StringLength(1000)]
        public string? QuestionText { get; set; }

        [Range(1, 100)]
        public int? Points { get; set; }

        public int? Order { get; set; }

        // If provided, replaces ALL existing options.
        // If omitted, options stay as they are.
        public List<CreateOptionDto>? Options { get; set; }
    }
}