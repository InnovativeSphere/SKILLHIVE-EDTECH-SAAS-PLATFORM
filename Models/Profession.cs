using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillHive.Models
{
    public class Profession
    {
        [Key]
        public int ProfessionId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public bool IsGlobal { get; set; } = false;

        public int? AcademyId { get; set; }

        [Column(TypeName = "text[]")]
        public List<string> PreviousNames { get; set; } = new();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        [ForeignKey(nameof(AcademyId))]
        public Academy? Academy { get; set; }
    }
}