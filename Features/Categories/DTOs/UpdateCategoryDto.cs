using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Categories.DTOs
{
    public class UpdateCategoryDto
    {
        [StringLength(100)]
        public string? Name { get; set; }
    }
}