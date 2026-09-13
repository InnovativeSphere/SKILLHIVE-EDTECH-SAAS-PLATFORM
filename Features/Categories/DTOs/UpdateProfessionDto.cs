using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Categories.DTOs
{
    public class UpdateProfessionDto
    {
        [StringLength(100)]
        public string? Name { get; set; }
    }
}