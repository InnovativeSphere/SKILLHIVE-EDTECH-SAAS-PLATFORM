using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Notifications.Services;

namespace SkillHive.Features.Notifications.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false)
        {
            var userId = JwtHelper.GetUserId(User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return ApiResponse.Success(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = JwtHelper.GetUserId(User);
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return ApiResponse.Success(new { unreadCount = count });
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = JwtHelper.GetUserId(User);
            var result = await _notificationService.MarkAsReadAsync(id, userId);

            if (!result)
                return ApiResponse.NotFound("Notification not found");

            return ApiResponse.Success(null, "Marked as read");
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = JwtHelper.GetUserId(User);
            await _notificationService.MarkAllAsReadAsync(userId);
            return ApiResponse.Success(null, "All notifications marked as read");
        }
    }
}