using System.ComponentModel.DataAnnotations;

namespace SkillHive.Features.Notifications.DTOs
{
    public class MarkReadDto
    {
        [Required]
        public int NotificationId { get; set; }
    }
}