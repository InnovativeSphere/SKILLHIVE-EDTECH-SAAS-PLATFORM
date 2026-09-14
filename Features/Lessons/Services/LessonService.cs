using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Lessons.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Lessons.Services
{
    public class LessonService
    {
        private readonly AppDbContext _db;

        public LessonService(AppDbContext db)
        {
            _db = db;
        }

        // ─── List Lessons for a Course ─────────────────────────────
        // Public listing for a course. Hides non-active lessons unless staff.
        public async Task<List<object>> GetCourseLessonsAsync(string courseSlug, bool isStaff, int? requesterAcademyId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Slug == courseSlug);
            if (course == null)
                throw new InvalidOperationException("Course not found");

            var isOwnerOfCourseAcademy = isStaff && requesterAcademyId == course.AcademyId;

            // Public access check
            if (!isOwnerOfCourseAcademy)
            {
                if (course.Status != CourseStatus.PUBLISHED)
                    throw new InvalidOperationException("Course not found");

                if (course.Visibility == CourseVisibility.PRIVATE)
                    throw new InvalidOperationException("Course not found");
            }

            var query = _db.Lessons.Where(l => l.CourseId == course.CourseId);

            if (!isOwnerOfCourseAcademy)
                query = query.Where(l => l.IsActive);

            var lessons = await query
                .OrderBy(l => l.Order)
                .Select(l => new
                {
                    lessonId = l.LessonId,
                    title = l.Title,
                    description = l.Description,
                    order = l.Order,
                    durationMinutes = l.DurationMinutes,
                    isPreview = l.IsPreview,
                    isActive = l.IsActive,
                    createdAt = l.CreatedAt,
                    updatedAt = l.UpdatedAt
                })
                .ToListAsync();

            return lessons.Cast<object>().ToList();
        }

        // ─── Get Single Lesson (with content) ──────────────────────
        public async Task<object> GetLessonAsync(int lessonId, bool isStaff, int? requesterAcademyId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            var isOwnerOfCourseAcademy = isStaff && requesterAcademyId == lesson.Course.AcademyId;

            if (!isOwnerOfCourseAcademy)
            {
                if (!lesson.IsActive)
                    throw new InvalidOperationException("Lesson not found");

                if (lesson.Course.Status != CourseStatus.PUBLISHED)
                    throw new InvalidOperationException("Lesson not found");

                // Non-staff can only read content of preview lessons OR enrolled students
                // (Enrollment check comes later — for now only preview lessons expose content)
                if (!lesson.IsPreview)
                    throw new InvalidOperationException("You must be enrolled to view this lesson's content");
            }

            return new
            {
                lessonId = lesson.LessonId,
                courseId = lesson.CourseId,
                title = lesson.Title,
                description = lesson.Description,
                content = lesson.Content,
                videoUrl = lesson.VideoUrl,
                order = lesson.Order,
                durationMinutes = lesson.DurationMinutes,
                isPreview = lesson.IsPreview,
                isActive = lesson.IsActive,
                createdAt = lesson.CreatedAt,
                updatedAt = lesson.UpdatedAt
            };
        }

        // ─── Create Lesson ─────────────────────────────────────────
        public async Task<object> CreateLessonAsync(int userId, UserRole role, int? academyId, int courseId, CreateLessonDto dto)
        {
            var course = await GetCourseForModificationAsync(userId, role, academyId, courseId);

            // Determine target order
            int targetOrder;
            if (dto.Order.HasValue && dto.Order.Value > 0)
            {
                targetOrder = dto.Order.Value;

                // Shift lessons at or after targetOrder by +1
                var toShift = await _db.Lessons
                    .Where(l => l.CourseId == courseId && l.Order >= targetOrder)
                    .ToListAsync();

                foreach (var l in toShift)
                    l.Order += 1;
            }
            else
            {
                // Append to end
                var maxOrder = await _db.Lessons
                    .Where(l => l.CourseId == courseId)
                    .MaxAsync(l => (int?)l.Order) ?? 0;

                targetOrder = maxOrder + 1;
            }

            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title)),
                Description = dto.Description != null ? Utils.SanitizeInput(dto.Description) : null,
                Content = dto.Content,
                VideoUrl = dto.VideoUrl,
                Order = targetOrder,
                DurationMinutes = dto.DurationMinutes,
                IsPreview = dto.IsPreview,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Lessons.Add(lesson);

            // Update course counter
            course.TotalLessons += 1;
            course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new
            {
                lessonId = lesson.LessonId,
                courseId = lesson.CourseId,
                title = lesson.Title,
                order = lesson.Order,
                isPreview = lesson.IsPreview,
                createdAt = lesson.CreatedAt
            };
        }

        // ─── Update Lesson ─────────────────────────────────────────
        public async Task<object> UpdateLessonAsync(int userId, UserRole role, int? academyId, int lessonId, UpdateLessonDto dto)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            await EnsureCanModifyAsync(userId, role, academyId, lesson.Course);

            if (!string.IsNullOrWhiteSpace(dto.Title))
                lesson.Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title));

            if (dto.Description != null)
                lesson.Description = Utils.SanitizeInput(dto.Description);

            if (dto.Content != null)
                lesson.Content = dto.Content;

            if (dto.VideoUrl != null)
                lesson.VideoUrl = dto.VideoUrl;

            if (dto.DurationMinutes.HasValue)
                lesson.DurationMinutes = dto.DurationMinutes;

            if (dto.IsPreview.HasValue)
                lesson.IsPreview = dto.IsPreview.Value;

            lesson.UpdatedAt = DateTime.UtcNow;
            lesson.Course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new
            {
                lessonId = lesson.LessonId,
                title = lesson.Title,
                order = lesson.Order,
                isPreview = lesson.IsPreview,
                updatedAt = lesson.UpdatedAt
            };
        }

        // ─── Reorder Lesson ────────────────────────────────────────
        public async Task<object> ReorderLessonAsync(int userId, UserRole role, int? academyId, int lessonId, int newOrder)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            await EnsureCanModifyAsync(userId, role, academyId, lesson.Course);

            if (newOrder == lesson.Order)
                return new { lessonId = lesson.LessonId, order = lesson.Order, message = "No change" };

            var oldOrder = lesson.Order;
            var courseId = lesson.CourseId;

            if (newOrder > oldOrder)
            {
                // Moving down: shift lessons between oldOrder+1 and newOrder up by -1
                var affected = await _db.Lessons
                    .Where(l => l.CourseId == courseId
                                && l.Order > oldOrder
                                && l.Order <= newOrder
                                && l.LessonId != lessonId)
                    .ToListAsync();

                foreach (var l in affected)
                    l.Order -= 1;
            }
            else
            {
                // Moving up: shift lessons between newOrder and oldOrder-1 down by +1
                var affected = await _db.Lessons
                    .Where(l => l.CourseId == courseId
                                && l.Order >= newOrder
                                && l.Order < oldOrder
                                && l.LessonId != lessonId)
                    .ToListAsync();

                foreach (var l in affected)
                    l.Order += 1;
            }

            lesson.Order = newOrder;
            lesson.UpdatedAt = DateTime.UtcNow;
            lesson.Course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new { lessonId = lesson.LessonId, order = lesson.Order, message = "Lesson reordered" };
        }

        // ─── Deactivate (Soft Delete) Lesson ───────────────────────
        public async Task<object> DeactivateLessonAsync(int userId, UserRole role, int? academyId, int lessonId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            await EnsureCanModifyAsync(userId, role, academyId, lesson.Course);

            if (!lesson.IsActive)
                throw new InvalidOperationException("Lesson is already inactive");

            var removedOrder = lesson.Order;
            var courseId = lesson.CourseId;

            lesson.IsActive = false;
            lesson.UpdatedAt = DateTime.UtcNow;

            var after = await _db.Lessons
                .Where(l => l.CourseId == courseId && l.Order > removedOrder)
                .ToListAsync();

            foreach (var l in after)
                l.Order -= 1;

            // Update course counter
            lesson.Course.TotalLessons = Math.Max(0, lesson.Course.TotalLessons - 1);
            lesson.Course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new { lessonId = lesson.LessonId, isActive = false, message = "Lesson deactivated" };
        }

        //  Helpers 
        private async Task<Course> GetCourseForModificationAsync(int userId, UserRole role, int? academyId, int courseId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
                throw new InvalidOperationException("Course not found");

            await EnsureCanModifyAsync(userId, role, academyId, course);
            return course;
        }

        private Task EnsureCanModifyAsync(int userId, UserRole role, int? academyId, Course course)
        {
            if (role == UserRole.ACADEMY_OWNER)
            {
                if (course.AcademyId != academyId)
                    throw new UnauthorizedAccessException("Course does not belong to your academy");
                return Task.CompletedTask;
            }

            if (role == UserRole.INSTRUCTOR)
            {
                if (course.InstructorId != userId)
                    throw new UnauthorizedAccessException("You can only modify lessons of courses you created");
                return Task.CompletedTask;
            }

            throw new UnauthorizedAccessException("You do not have permission to modify lessons");
        }
    }
}