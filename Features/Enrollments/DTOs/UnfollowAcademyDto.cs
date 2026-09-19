using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Enrollments.DTOs
{
    public class UnfollowAcademyDto
    {
        [Required]
        public int AcademyId { get; set; }
    }
}