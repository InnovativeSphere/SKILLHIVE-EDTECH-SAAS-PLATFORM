using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Courses.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Courses.Services
{
    public class CourseService
    {
        private readonly AppDbContext _db;

        public CourseService(AppDbContext db)
        {
            _db = db;
        }

        // ─── Public Browse ─────────────────────────────────────────
        public async Task<object> BrowseCoursesAsync(
            int? categoryId,
            int? professionId,
            int? academyId,
            CourseLevel? level,
            bool? isFree,
            string? search,
            int? page,
            int? pageSize)
        {
            var query = _db.Courses
                .Include(c => c.Academy)
                .Include(c => c.Instructor)
                .Include(c => c.Profession)
                .Where(c => c.Status == CourseStatus.PUBLISHED
                            && c.Visibility == CourseVisibility.PUBLIC
                            && c.Academy.IsActive);

            if (categoryId.HasValue)
                query = query.Where(c => c.Profession.CategoryId == categoryId.Value);

            if (professionId.HasValue)
                query = query.Where(c => c.ProfessionId == professionId.Value);

            if (academyId.HasValue)
                query = query.Where(c => c.AcademyId == academyId.Value);

            if (level.HasValue)
                query = query.Where(c => c.Level == level.Value);

            if (isFree.HasValue)
                query = query.Where(c => c.IsFree == isFree.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(pattern) ||
                    (c.Description != null && c.Description.ToLower().Contains(pattern)));
            }

            var ordered = query.OrderByDescending(c => c.PublishedAt);

            var (pageNumber, size) = PaginationHelper.Normalize(page, pageSize);
            var total = await ordered.CountAsync();

            var items = await ordered
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title,
                    slug = c.Slug,
                    description = c.Description,
                    coverImageUrl = c.CoverImageUrl,
                    isFree = c.IsFree,
                    price = c.Price,
                    discountPrice = c.DiscountPrice,
                    currency = c.Currency,
                    level = c.Level.ToString(),
                    language = c.Language,
                    totalLessons = c.TotalLessons,
                    totalEnrollments = c.TotalEnrollments,
                    averageRating = c.AverageRating,
                    totalReviews = c.TotalReviews,
                    publishedAt = c.PublishedAt,
                    academy = new
                    {
                        academyId = c.Academy.AcademyId,
                        name = c.Academy.Name,
                        slug = c.Academy.Slug,
                        logoUrl = c.Academy.LogoUrl
                    },
                    instructor = new
                    {
                        userId = c.Instructor.UserId,
                        fullName = c.Instructor.FullName
                    },
                    profession = new
                    {
                        professionId = c.Profession.ProfessionId,
                        name = c.Profession.Name,
                        slug = c.Profession.Slug,
                        categoryId = c.Profession.CategoryId
                    }
                })
                .ToListAsync();

            return new
            {
                items,
                pagination = new
                {
                    totalRecords = total,
                    pageNumber,
                    pageSize = size,
                    totalPages = (int)Math.Ceiling((double)total / size)
                }
            };
        }

        // ─── Get Single Course ────────────────────────────────────
        public async Task<object> GetCourseAsync(string slug, int? requesterAcademyId, bool isAcademyStaff)
        {
            var course = await _db.Courses
                .Include(c => c.Academy)
                .Include(c => c.Instructor)
                .Include(c => c.Profession)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.Slug == slug);

            if (course == null)
                throw new InvalidOperationException("Course not found");

            // Access check
            var isOwnerOfCourseAcademy = isAcademyStaff && requesterAcademyId == course.AcademyId;

            if (!isOwnerOfCourseAcademy)
            {
                // Public visibility rules
                if (course.Status != CourseStatus.PUBLISHED)
                    throw new InvalidOperationException("Course not found");

                if (course.Visibility == CourseVisibility.PRIVATE)
                    throw new InvalidOperationException("Course not found");

                if (!course.Academy.IsActive)
                    throw new InvalidOperationException("Course not found");
            }

            return new
            {
                courseId = course.CourseId,
                title = course.Title,
                slug = course.Slug,
                description = course.Description,
                longDescription = course.LongDescription,
                coverImageUrl = course.CoverImageUrl,
                promoVideoUrl = course.PromoVideoUrl,
                isFree = course.IsFree,
                price = course.Price,
                discountPrice = course.DiscountPrice,
                currency = course.Currency,
                level = course.Level.ToString(),
                language = course.Language,
                estimatedDurationMinutes = course.EstimatedDurationMinutes,
                prerequisites = course.Prerequisites,
                status = course.Status.ToString(),
                visibility = course.Visibility.ToString(),
                publishedAt = course.PublishedAt,
                totalLessons = course.TotalLessons,
                totalEnrollments = course.TotalEnrollments,
                averageRating = course.AverageRating,
                totalReviews = course.TotalReviews,
                createdAt = course.CreatedAt,
                updatedAt = course.UpdatedAt,
                academy = new
                {
                    academyId = course.Academy.AcademyId,
                    name = course.Academy.Name,
                    slug = course.Academy.Slug,
                    logoUrl = course.Academy.LogoUrl
                },
                instructor = new
                {
                    userId = course.Instructor.UserId,
                    fullName = course.Instructor.FullName
                },
                profession = new
                {
                    professionId = course.Profession.ProfessionId,
                    name = course.Profession.Name,
                    slug = course.Profession.Slug,
                    category = new
                    {
                        categoryId = course.Profession.Category.CategoryId,
                        name = course.Profession.Category.Name,
                        slug = course.Profession.Category.Slug
                    }
                }
            };
        }

        // ─── List Own Academy's Courses (Dashboard) ───────────────
        public async Task<List<object>> GetAcademyCoursesAsync(int? academyId)
        {
            if (academyId == null)
                throw new InvalidOperationException("You do not belong to an academy");

            var courses = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Profession)
                .Where(c => c.AcademyId == academyId.Value)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title,
                    slug = c.Slug,
                    status = c.Status.ToString(),
                    visibility = c.Visibility.ToString(),
                    isFree = c.IsFree,
                    price = c.Price,
                    totalLessons = c.TotalLessons,
                    totalEnrollments = c.TotalEnrollments,
                    averageRating = c.AverageRating,
                    totalReviews = c.TotalReviews,
                    publishedAt = c.PublishedAt,
                    updatedAt = c.UpdatedAt,
                    instructor = new
                    {
                        userId = c.Instructor.UserId,
                        fullName = c.Instructor.FullName
                    },
                    profession = new
                    {
                        professionId = c.Profession.ProfessionId,
                        name = c.Profession.Name
                    }
                })
                .ToListAsync();

            return courses.Cast<object>().ToList();
        }

        // ─── Create Course (Instructor or Owner) ──────────────────
        public async Task<object> CreateCourseAsync(int userId, UserRole role, int? academyId, CreateCourseDto dto)
        {
            if (role != UserRole.ACADEMY_OWNER && role != UserRole.INSTRUCTOR)
                throw new UnauthorizedAccessException("Only instructors and academy owners can create courses");

            if (academyId == null)
                throw new InvalidOperationException("You do not belong to an academy");

            var profession = await _db.Professions
                .FirstOrDefaultAsync(p => p.ProfessionId == dto.ProfessionId && p.IsActive);

            if (profession == null)
                throw new InvalidOperationException("Profession not found or inactive");

            // Must be global profession OR belong to same academy
            if (!profession.IsGlobal && profession.AcademyId != academyId)
                throw new InvalidOperationException("You can only use professions available to your academy");

            // Price consistency
            if (!dto.IsFree && (dto.Price == null || dto.Price <= 0))
                throw new InvalidOperationException("Paid courses must have a price");

            var title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title));
            var baseSlug = SlugHelper.GenerateSlug(title);

            var slug = await SlugHelper.EnsureUniqueSlugAsync(
                baseSlug,
                async s => await _db.Courses.AnyAsync(c => c.Slug == s && c.AcademyId == academyId.Value));

            var course = new Course
            {
                Title = title,
                Slug = slug,
                Description = dto.Description != null ? Utils.SanitizeInput(dto.Description) : null,
                LongDescription = dto.LongDescription != null ? Utils.SanitizeInput(dto.LongDescription) : null,
                AcademyId = academyId.Value,
                InstructorId = userId,
                ProfessionId = dto.ProfessionId,
                CreatedByUserId = userId,
                IsFree = dto.IsFree,
                Price = dto.IsFree ? null : dto.Price,
                DiscountPrice = dto.IsFree ? null : dto.DiscountPrice,
                CoverImageUrl = dto.CoverImageUrl,
                PromoVideoUrl = dto.PromoVideoUrl,
                Level = dto.Level,
                Language = dto.Language,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                Prerequisites = dto.Prerequisites != null ? Utils.SanitizeInput(dto.Prerequisites) : null,
                Status = CourseStatus.DRAFT,
                Visibility = CourseVisibility.PUBLIC,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return new
            {
                courseId = course.CourseId,
                title = course.Title,
                slug = course.Slug,
                status = course.Status.ToString(),
                visibility = course.Visibility.ToString(),
                isFree = course.IsFree,
                price = course.Price,
                createdAt = course.CreatedAt
            };
        }

        // ─── Update Course ────────────────────────────────────────
        public async Task<object> UpdateCourseAsync(int userId, UserRole role, int? academyId, int courseId, UpdateCourseDto dto)
        {
            var course = await GetCourseForModificationAsync(userId, role, academyId, courseId);

            if (course.Status == CourseStatus.ARCHIVED)
                throw new InvalidOperationException("Cannot edit an archived course");

            // Title → regenerate slug
            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                var newTitle = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title));
                if (!string.Equals(newTitle, course.Title, StringComparison.OrdinalIgnoreCase))
                {
                    course.Title = newTitle;

                    var baseSlug = SlugHelper.GenerateSlug(newTitle);
                    course.Slug = await SlugHelper.EnsureUniqueSlugAsync(
                        baseSlug,
                        async s => await _db.Courses.AnyAsync(c => c.Slug == s && c.AcademyId == course.AcademyId && c.CourseId != courseId));
                }
            }

            if (dto.Description != null)
                course.Description = Utils.SanitizeInput(dto.Description);

            if (dto.LongDescription != null)
                course.LongDescription = Utils.SanitizeInput(dto.LongDescription);

            if (dto.ProfessionId.HasValue)
            {
                var profession = await _db.Professions
                    .FirstOrDefaultAsync(p => p.ProfessionId == dto.ProfessionId.Value && p.IsActive);

                if (profession == null)
                    throw new InvalidOperationException("Profession not found or inactive");

                if (!profession.IsGlobal && profession.AcademyId != course.AcademyId)
                    throw new InvalidOperationException("You can only use professions available to your academy");

                course.ProfessionId = dto.ProfessionId.Value;
            }

            if (dto.IsFree.HasValue)
            {
                course.IsFree = dto.IsFree.Value;

                if (course.IsFree)
                {
                    course.Price = null;
                    course.DiscountPrice = null;
                }
            }

            if (!course.IsFree)
            {
                if (dto.Price.HasValue) course.Price = dto.Price;
                if (dto.DiscountPrice.HasValue) course.DiscountPrice = dto.DiscountPrice;

                if (course.Price == null || course.Price <= 0)
                    throw new InvalidOperationException("Paid courses must have a price");
            }

            if (dto.CoverImageUrl != null) course.CoverImageUrl = dto.CoverImageUrl;
            if (dto.PromoVideoUrl != null) course.PromoVideoUrl = dto.PromoVideoUrl;
            if (dto.Level.HasValue) course.Level = dto.Level.Value;
            if (dto.Language != null) course.Language = dto.Language;
            if (dto.EstimatedDurationMinutes.HasValue) course.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
            if (dto.Prerequisites != null) course.Prerequisites = Utils.SanitizeInput(dto.Prerequisites);
            if (dto.Visibility.HasValue) course.Visibility = dto.Visibility.Value;

            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                courseId = course.CourseId,
                title = course.Title,
                slug = course.Slug,
                status = course.Status.ToString(),
                visibility = course.Visibility.ToString(),
                isFree = course.IsFree,
                price = course.Price,
                updatedAt = course.UpdatedAt
            };
        }

        // ─── Submit for Review ────────────────────────────────────
        public async Task<object> SubmitForReviewAsync(int userId, UserRole role, int? academyId, int courseId)
        {
            var course = await GetCourseForModificationAsync(userId, role, academyId, courseId);

            if (course.Status != CourseStatus.DRAFT && course.Status != CourseStatus.REJECTED)
                throw new InvalidOperationException("Only draft or rejected courses can be submitted for review");

            course.Status = CourseStatus.PENDING_REVIEW;
            course.RejectionReason = null;
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { courseId = course.CourseId, status = course.Status.ToString(), message = "Course submitted for review" };
        }

        // ─── Approve Course (Owner only) ──────────────────────────
        public async Task<object> ApproveCourseAsync(int userId, UserRole role, int? academyId, int courseId)
        {
            if (role != UserRole.ACADEMY_OWNER)
                throw new UnauthorizedAccessException("Only academy owners can approve courses");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null) throw new InvalidOperationException("Course not found");
            if (course.AcademyId != academyId) throw new UnauthorizedAccessException("Course does not belong to your academy");

            if (course.Status != CourseStatus.PENDING_REVIEW)
                throw new InvalidOperationException("Only courses pending review can be approved");

            course.Status = CourseStatus.PUBLISHED;
            course.PublishedAt = DateTime.UtcNow;
            course.RejectionReason = null;
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { courseId = course.CourseId, status = course.Status.ToString(), publishedAt = course.PublishedAt };
        }

        // ─── Reject Course (Owner only) ───────────────────────────
        public async Task<object> RejectCourseAsync(int userId, UserRole role, int? academyId, int courseId, string reason)
        {
            if (role != UserRole.ACADEMY_OWNER)
                throw new UnauthorizedAccessException("Only academy owners can reject courses");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null) throw new InvalidOperationException("Course not found");
            if (course.AcademyId != academyId) throw new UnauthorizedAccessException("Course does not belong to your academy");

            if (course.Status != CourseStatus.PENDING_REVIEW)
                throw new InvalidOperationException("Only courses pending review can be rejected");

            course.Status = CourseStatus.REJECTED;
            course.RejectionReason = Utils.SanitizeInput(reason);
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { courseId = course.CourseId, status = course.Status.ToString(), rejectionReason = course.RejectionReason };
        }

        // ─── Archive Course (Owner only) ──────────────────────────
        public async Task<object> ArchiveCourseAsync(int userId, UserRole role, int? academyId, int courseId)
        {
            if (role != UserRole.ACADEMY_OWNER)
                throw new UnauthorizedAccessException("Only academy owners can archive courses");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null) throw new InvalidOperationException("Course not found");
            if (course.AcademyId != academyId) throw new UnauthorizedAccessException("Course does not belong to your academy");

            if (course.Status == CourseStatus.ARCHIVED)
                throw new InvalidOperationException("Course is already archived");

            course.Status = CourseStatus.ARCHIVED;
            course.ArchivedAt = DateTime.UtcNow;
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { courseId = course.CourseId, status = course.Status.ToString(), archivedAt = course.ArchivedAt };
        }

        // ─── Helper: Fetch course with permission check ───────────
        private async Task<Course> GetCourseForModificationAsync(int userId, UserRole role, int? academyId, int courseId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
                throw new InvalidOperationException("Course not found");

            // Owner: can modify any course in their academy
            if (role == UserRole.ACADEMY_OWNER)
            {
                if (course.AcademyId != academyId)
                    throw new UnauthorizedAccessException("Course does not belong to your academy");
                return course;
            }

            // Instructor: can only modify their own courses
            if (role == UserRole.INSTRUCTOR)
            {
                if (course.InstructorId != userId)
                    throw new UnauthorizedAccessException("You can only modify courses you created");
                return course;
            }

            throw new UnauthorizedAccessException("You do not have permission to modify courses");
        }
    }
}