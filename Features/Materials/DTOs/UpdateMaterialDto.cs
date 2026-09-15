using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Materials.DTOs
{
    public class UpdateMaterialDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public int? Order { get; set; }
    }
}