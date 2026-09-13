using Microsoft.EntityFrameworkCore;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Email.Services;
using SkillHive.Models;

namespace SkillHive.Features.Notifications.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _db;
        private readonly EmailService _email;

        public NotificationService(AppDbContext db, EmailService email)
        {
            _db = db;
            _email = email;
        }

        /// <summary>
        /// Core dispatcher. Stores an in-app notification, and optionally sends an email.
        /// </summary>
        public async Task NotifyAsync(
            int userId,
            NotificationType type,
            string title,
            string message,
            bool sendEmail = false,
            string? emailTemplate = null,
            object? emailModel = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Channel = sendEmail ? "IN_APP_EMAIL" : "IN_APP",
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            if (sendEmail && !string.IsNullOrEmpty(emailTemplate) && emailModel != null)
            {
                var user = await _db.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _email.SendAsync(user.Email, title, emailTemplate, emailModel);
                }
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            var query = _db.Notifications.Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (notifications.Count == 0)
                return false;

            foreach (var n in notifications)
                n.IsRead = true;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}