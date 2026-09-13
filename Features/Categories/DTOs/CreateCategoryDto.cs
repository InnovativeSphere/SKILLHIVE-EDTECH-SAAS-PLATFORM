using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Categories.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Only used when a superadmin creates a global category
        // Academy owners are forced to create scoped ones automatically
        public bool IsGlobal { get; set; } = false;
    }
}