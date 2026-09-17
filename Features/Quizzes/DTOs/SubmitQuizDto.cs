using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Quizzes.DTOs
{
    public class SubmitQuizDto
    {
        [Required]
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }

    public class SubmitAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }

        // Null = student did not answer this question (counts as wrong)
        public int? OptionId { get; set; }
    }
}