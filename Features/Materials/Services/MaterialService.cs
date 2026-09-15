using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Materials.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Materials.Services
{
    public class MaterialService
    {
        private readonly AppDbContext _db;
        private readonly CloudinaryService _cloudinary;

        public MaterialService(AppDbContext db, CloudinaryService cloudinary)
        {
            _db = db;
            _cloudinary = cloudinary;
        }

        // ─── File Upload ────────────────────────────────────────────
        public async Task<object> UploadFileAsync(Stream fileStream, string fileName)
        {
            if (!FileHelper.IsAllowedFile(fileName, null))
                throw new InvalidOperationException("File type not allowed");

            const string folder = "skillhive/course-materials";

            var result = await _cloudinary.UploadAsync(fileStream, fileName, folder);
            var fileType = FileHelper.GetFileTypeCategory(fileName);

            return new
            {
                fileUrl = result.Url,
                publicId = result.PublicId,
                fileSizeBytes = result.Bytes,
                fileType = fileType.ToString(),
                originalFileName = fileName,
                formattedSize = FileHelper.FormatFileSize(result.Bytes)
            };
        }

        // ─── List Materials for a Lesson ────────────────────────────
        public async Task<List<object>> GetForLessonAsync(int lessonId, bool isStaff, int? requesterAcademyId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            var isOwnerOfCourseAcademy = isStaff && requesterAcademyId == lesson.Course.AcademyId;

            // Non-staff can only see materials of a published, non-private course
            if (!isOwnerOfCourseAcademy)
            {
                if (lesson.Course.Status != CourseStatus.PUBLISHED
                    || lesson.Course.Visibility == CourseVisibility.PRIVATE
                    || !lesson.IsActive)
                {
                    throw new InvalidOperationException("Lesson not found");
                }
            }

            var query = _db.Materials.Where(m => m.LessonId == lessonId);

            if (!isOwnerOfCourseAcademy)
                query = query.Where(m => m.IsActive);

            var materials = await query
                .OrderBy(m => m.Order)
                .Select(m => new
                {
                    materialId = m.MaterialId,
                    lessonId = m.LessonId,
                    title = m.Title,
                    fileUrl = m.FileUrl,
                    fileType = m.FileType.ToString(),
                    fileSizeBytes = m.FileSizeBytes,
                    order = m.Order,
                    isActive = m.IsActive,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            return materials.Cast<object>().ToList();
        }

        // ─── Create Material ────────────────────────────────────────
        public async Task<object> CreateAsync(
            int userId, UserRole role, int? academyId, int lessonId, CreateMaterialDto dto)
        {
            var lesson = await GetLessonForModificationAsync(userId, role, academyId, lessonId);

            if (!Enum.TryParse<FileType>(dto.FileType.ToUpperInvariant(), out var fileType))
                throw new InvalidOperationException("Invalid file type");

            int targetOrder;

            if (dto.Order.HasValue && dto.Order.Value > 0)
            {
                targetOrder = dto.Order.Value;

                var toShift = await _db.Materials
                    .Where(m => m.LessonId == lessonId && m.Order >= targetOrder)
                    .ToListAsync();

                foreach (var m in toShift)
                    m.Order += 1;
            }
            else
            {
                var maxOrder = await _db.Materials
                    .Where(m => m.LessonId == lessonId)
                    .MaxAsync(m => (int?)m.Order) ?? 0;

                targetOrder = maxOrder + 1;
            }

            var material = new Material
            {
                LessonId = lessonId,
                Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title)),
                FileUrl = dto.FileUrl,
                FileType = fileType,
                FileSizeBytes = dto.FileSizeBytes,
                Order = targetOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Materials.Add(material);
            lesson.UpdatedAt = DateTime.UtcNow;
            lesson.Course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                materialId = material.MaterialId,
                lessonId = material.LessonId,
                title = material.Title,
                fileUrl = material.FileUrl,
                fileType = material.FileType.ToString(),
                fileSizeBytes = material.FileSizeBytes,
                order = material.Order,
                createdAt = material.CreatedAt
            };
        }

        // ─── Update Material ────────────────────────────────────────
        public async Task<object> UpdateAsync(
            int userId, UserRole role, int? academyId, int materialId, UpdateMaterialDto dto)
        {
            var material = await _db.Materials
                .Include(m => m.Lesson)
                    .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(m => m.MaterialId == materialId);

            if (material == null)
                throw new InvalidOperationException("Material not found");

            await EnsureCanModifyAsync(userId, role, academyId, material.Lesson.Course);

            if (!string.IsNullOrWhiteSpace(dto.Title))
                material.Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title));

            if (dto.Order.HasValue && dto.Order.Value > 0 && dto.Order.Value != material.Order)
            {
                var oldOrder = material.Order;
                var newOrder = dto.Order.Value;
                var lessonId = material.LessonId;

                if (newOrder > oldOrder)
                {
                    var affected = await _db.Materials
                        .Where(m => m.LessonId == lessonId
                                    && m.Order > oldOrder
                                    && m.Order <= newOrder
                                    && m.MaterialId != materialId)
                        .ToListAsync();

                    foreach (var m in affected) m.Order -= 1;
                }
                else
                {
                    var affected = await _db.Materials
                        .Where(m => m.LessonId == lessonId
                                    && m.Order >= newOrder
                                    && m.Order < oldOrder
                                    && m.MaterialId != materialId)
                        .ToListAsync();

                    foreach (var m in affected) m.Order += 1;
                }

                material.Order = newOrder;
            }

            material.Lesson.UpdatedAt = DateTime.UtcNow;
            material.Lesson.Course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                materialId = material.MaterialId,
                title = material.Title,
                order = material.Order,
                fileUrl = material.FileUrl,
                fileType = material.FileType.ToString()
            };
        }

        // ─── Deactivate Material (Soft Delete) ──────────────────────
        public async Task<object> DeactivateAsync(int userId, UserRole role, int? academyId, int materialId)
        {
            var material = await _db.Materials
                .Include(m => m.Lesson)
                    .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(m => m.MaterialId == materialId);

            if (material == null)
                throw new InvalidOperationException("Material not found");

            await EnsureCanModifyAsync(userId, role, academyId, material.Lesson.Course);

            if (!material.IsActive)
                throw new InvalidOperationException("Material is already inactive");

            var removedOrder = material.Order;
            var lessonId = material.LessonId;

            material.IsActive = false;

            // Close the gap
            var after = await _db.Materials
                .Where(m => m.LessonId == lessonId && m.Order > removedOrder)
                .ToListAsync();

            foreach (var m in after)
                m.Order -= 1;

            material.Lesson.UpdatedAt = DateTime.UtcNow;
            material.Lesson.Course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { materialId = material.MaterialId, isActive = false, message = "Material deactivated" };
        }

        // ─── Helpers ────────────────────────────────────────────────
        private async Task<Lesson> GetLessonForModificationAsync(
            int userId, UserRole role, int? academyId, int lessonId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            await EnsureCanModifyAsync(userId, role, academyId, lesson.Course);
            return lesson;
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

            throw new UnauthorizedAccessException("You do not have permission to modify materials");
        }
    }
}