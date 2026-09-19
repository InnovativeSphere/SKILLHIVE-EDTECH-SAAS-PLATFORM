using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Certificates.DTOs
{
    public class RevokeCertificateDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}