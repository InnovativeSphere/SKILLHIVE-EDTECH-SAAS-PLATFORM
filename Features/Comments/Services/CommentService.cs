using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Comments.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Comments.Services
{
    public class CommentService
    {
        private readonly AppDbContext _db;

        public CommentService(AppDbContext db)
        {
            _db = db;
        }

        // ─── Create Comment ────────────────────────────────────────
        public async Task<object> CreateCommentAsync(
            int userId, UserRole role, int? academyId, CreateCommentDto dto)
        {
            // Resolve the target and its owning course
            var (courseId, targetExists) = await ResolveTargetAsync(dto.TargetType, dto.TargetId);
            if (!targetExists)
                throw new InvalidOperationException("Target not found");

            // Author must be able to comment
            var canComment = await CanUserCommentAsync(userId, role, academyId, courseId);
            if (!canComment)
                throw new UnauthorizedAccessException(
                    "You must be enrolled in this course to comment");

            // If it's a reply, ensure parent exists and is top-level
            if (dto.ParentCommentId.HasValue)
            {
                var parent = await _db.Comments
                    .FirstOrDefaultAsync(c => c.CommentId == dto.ParentCommentId.Value);

                if (parent == null)
                    throw new InvalidOperationException("Parent comment not found");

                if (parent.ParentCommentId != null)
                    throw new InvalidOperationException(
                        "Replies to replies are not allowed (max 2 levels)");

                if (parent.CourseId != courseId)
                    throw new InvalidOperationException("Parent comment belongs to a different course");
            }

            var comment = new Comment
            {
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                CourseId = courseId,
                AuthorId = userId,
                ParentCommentId = dto.ParentCommentId,
                Body = Utils.SanitizeInput(dto.Body),
                Status = CommentStatus.PUBLISHED,
                IsPinned = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return new
            {
                commentId = comment.CommentId,
                targetType = comment.TargetType.ToString(),
                targetId = comment.TargetId,
                courseId = comment.CourseId,
                parentCommentId = comment.ParentCommentId,
                body = comment.Body,
                status = comment.Status.ToString(),
                isPinned = comment.IsPinned,
                createdAt = comment.CreatedAt
            };
        }

        // ─── List Comments for a Target (threaded) ─────────────────
        public async Task<List<object>> ListCommentsAsync(
            CommentTargetType targetType, int targetId)
        {
            var (courseId, targetExists) = await ResolveTargetAsync(targetType, targetId);
            if (!targetExists)
                throw new InvalidOperationException("Target not found");

            // Fetch all published comments for this target
            var all = await _db.Comments
                .Include(c => c.Author)
                .Where(c => c.TargetType == targetType
                            && c.TargetId == targetId
                            && c.Status == CommentStatus.PUBLISHED)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Group top-level + replies
            var topLevel = all.Where(c => c.ParentCommentId == null).ToList();
            var replies = all.Where(c => c.ParentCommentId != null).ToList();

            var result = topLevel
                .OrderByDescending(c => c.IsPinned)
                .ThenBy(c => c.CreatedAt)
                .Select(t => new
                {
                    commentId = t.CommentId,
                    body = t.Body,
                    isPinned = t.IsPinned,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt,
                    author = new
                    {
                        userId = t.Author.UserId,
                        fullName = t.Author.FullName
                    },
                    replies = replies
                        .Where(r => r.ParentCommentId == t.CommentId)
                        .OrderBy(r => r.CreatedAt)
                        .Select(r => new
                        {
                            commentId = r.CommentId,
                            body = r.Body,
                            createdAt = r.CreatedAt,
                            updatedAt = r.UpdatedAt,
                            author = new
                            {
                                userId = r.Author.UserId,
                                fullName = r.Author.FullName
                            }
                        })
                        .ToList()
                })
                .ToList();

            return result.Cast<object>().ToList();
        }

        // ─── Get One Comment ───────────────────────────────────────
        public async Task<object> GetCommentAsync(int commentId)
        {
            var comment = await _db.Comments
                .Include(c => c.Author)
                .Include(c => c.Course)
                    .ThenInclude(c => c.Academy)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            return new
            {
                commentId = comment.CommentId,
                targetType = comment.TargetType.ToString(),
                targetId = comment.TargetId,
                parentCommentId = comment.ParentCommentId,
                body = comment.Body,
                status = comment.Status.ToString(),
                isPinned = comment.IsPinned,
                createdAt = comment.CreatedAt,
                updatedAt = comment.UpdatedAt,
                author = new
                {
                    userId = comment.Author.UserId,
                    fullName = comment.Author.FullName
                },
                course = new
                {
                    courseId = comment.Course.CourseId,
                    title = comment.Course.Title,
                    slug = comment.Course.Slug,
                    academy = new
                    {
                        academyId = comment.Course.Academy.AcademyId,
                        name = comment.Course.Academy.Name
                    }
                }
            };
        }

        // ─── Update Own Comment ────────────────────────────────────
        public async Task<object> UpdateCommentAsync(
            int userId, int commentId, UpdateCommentDto dto)
        {
            var comment = await _db.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("You can only edit your own comments");

            if (comment.Status == CommentStatus.DELETED)
                throw new InvalidOperationException("Cannot edit a deleted comment");

            comment.Body = Utils.SanitizeInput(dto.Body);
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                commentId = comment.CommentId,
                body = comment.Body,
                updatedAt = comment.UpdatedAt
            };
        }

        // ─── Delete Own Comment (soft) ─────────────────────────────
        public async Task<object> DeleteOwnCommentAsync(int userId, int commentId)
        {
            var comment = await _db.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("You can only delete your own comments");

            if (comment.Status == CommentStatus.DELETED)
                throw new InvalidOperationException("Comment is already deleted");

            // Block deletion if this is a top-level comment with replies
            var hasReplies = await _db.Comments
                .AnyAsync(c => c.ParentCommentId == commentId && c.Status == CommentStatus.PUBLISHED);

            if (hasReplies)
                throw new InvalidOperationException(
                    "Cannot delete a comment that has replies. Hide it instead.");

            comment.Status = CommentStatus.DELETED;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { commentId = comment.CommentId, message = "Comment deleted" };
        }

        // ─── Hide Comment (Owner/Moderator/Superadmin) ─────────────
        public async Task<object> HideCommentAsync(
            int commentId, UserRole role, int? academyId, string reason)
        {
            var comment = await _db.Comments
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            EnsureCanModerate(role, academyId, comment.Course.AcademyId);

            if (comment.Status == CommentStatus.HIDDEN)
                throw new InvalidOperationException("Comment is already hidden");

            comment.Status = CommentStatus.HIDDEN;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                commentId = comment.CommentId,
                status = comment.Status.ToString(),
                reason = Utils.SanitizeInput(reason),
                message = "Comment hidden"
            };
        }

        // ─── Unhide Comment ────────────────────────────────────────
        public async Task<object> UnhideCommentAsync(
            int commentId, UserRole role, int? academyId)
        {
            var comment = await _db.Comments
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            EnsureCanModerate(role, academyId, comment.Course.AcademyId);

            if (comment.Status != CommentStatus.HIDDEN)
                throw new InvalidOperationException("Comment is not hidden");

            comment.Status = CommentStatus.PUBLISHED;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                commentId = comment.CommentId,
                status = comment.Status.ToString(),
                message = "Comment restored"
            };
        }

        // ─── Pin/Unpin Comment (Owner/Moderator) ───────────────────
        public async Task<object> PinCommentAsync(
            int commentId, UserRole role, int? academyId, bool isPinned)
        {
            var comment = await _db.Comments
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            EnsureCanModerate(role, academyId, comment.Course.AcademyId);

            // Only top-level comments can be pinned
            if (comment.ParentCommentId != null)
                throw new InvalidOperationException("Only top-level comments can be pinned");

            if (comment.Status != CommentStatus.PUBLISHED)
                throw new InvalidOperationException("Only published comments can be pinned");

            comment.IsPinned = isPinned;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                commentId = comment.CommentId,
                isPinned = comment.IsPinned,
                message = isPinned ? "Comment pinned" : "Comment unpinned"
            };
        }

        // ─── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Given a target type and ID, returns the owning courseId and whether the target exists.
        /// </summary>
        private async Task<(int courseId, bool exists)> ResolveTargetAsync(
            CommentTargetType targetType, int targetId)
        {
            if (targetType == CommentTargetType.COURSE)
            {
                var course = await _db.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == targetId);
                return (targetId, course != null);
            }

            if (targetType == CommentTargetType.LESSON)
            {
                var lesson = await _db.Lessons
                    .FirstOrDefaultAsync(l => l.LessonId == targetId);
                return (lesson?.CourseId ?? 0, lesson != null);
            }

            return (0, false);
        }

        /// <summary>
        /// A user can comment if:
        /// - They are enrolled in the course, OR
        /// - They are academy staff (owner/instructor/moderator) of the owning academy
        /// </summary>
        private async Task<bool> CanUserCommentAsync(
            int userId, UserRole role, int? academyId, int courseId)
        {
            if (role == UserRole.SUPER_ADMIN)
                return true;

            if (role == UserRole.ACADEMY_OWNER || role == UserRole.INSTRUCTOR || role == UserRole.MODERATOR)
            {
                var course = await _db.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course != null && course.AcademyId == academyId)
                    return true;
            }

            // Otherwise, enrollment check
            return await _db.Enrollments
                .AnyAsync(e => e.StudentId == userId
                               && e.CourseId == courseId
                               && e.Status != EnrollmentStatus.DROPPED);
        }

        private static void EnsureCanModerate(
            UserRole role, int? requesterAcademyId, int courseAcademyId)
        {
            if (role == UserRole.SUPER_ADMIN)
                return;

            if (role == UserRole.ACADEMY_OWNER || role == UserRole.MODERATOR)
            {
                if (requesterAcademyId != courseAcademyId)
                    throw new UnauthorizedAccessException("Comment belongs to another academy");
                return;
            }

            throw new UnauthorizedAccessException(
                "Only academy owners, moderators, or superadmins can moderate comments");
        }
    }
}