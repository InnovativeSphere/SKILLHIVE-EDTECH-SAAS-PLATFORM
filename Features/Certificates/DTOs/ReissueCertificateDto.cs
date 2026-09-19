using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Certificates.DTOs
{
    public class ReissueCertificateDto
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}