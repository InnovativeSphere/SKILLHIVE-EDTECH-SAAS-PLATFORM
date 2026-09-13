using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Categories.DTOs
{
    public class CreateProfessionDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public bool IsGlobal { get; set; } = false;
    }
}