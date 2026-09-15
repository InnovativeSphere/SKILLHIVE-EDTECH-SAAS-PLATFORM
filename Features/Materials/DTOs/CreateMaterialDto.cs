using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Materials.DTOs
{
    public class CreateMaterialDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty; // PDF, DOCX, PPTX, VIDEO, IMAGE, TEXT, OTHER

        public long? FileSizeBytes { get; set; }

        public int? Order { get; set; }
    }
}