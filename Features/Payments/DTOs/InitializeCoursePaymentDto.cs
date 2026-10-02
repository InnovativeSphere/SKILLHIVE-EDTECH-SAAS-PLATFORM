using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Payments.DTOs
{
    public class InitializeCoursePaymentDto
    {
        [Required]
        public int CourseId { get; set; }
    }
}