using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Reviews.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Reviews.Services
{
    public class ReviewService
    {
        private readonly AppDbContext _db;

        public ReviewService(AppDbContext db)
        {
            _db = db;
        }

        // ─── Create Review (Student) ────────────────────────────────
        public async Task<object> CreateReviewAsync(int userId, CreateReviewDto dto)
        {
            // Resolve the enrollment
            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e =>
                    e.StudentId == userId && e.CourseId == dto.CourseId);

            if (enrollment == null)
                throw new InvalidOperationException("You are not enrolled in this course");

            if (enrollment.Status == EnrollmentStatus.DROPPED)
                throw new InvalidOperationException("Your enrollment has been dropped");

            if (enrollment.ProgressPercentage < 50)
                throw new InvalidOperationException(
                    "You must complete at least 50% of the course before leaving a review");

            var existing = await _db.Reviews
                .FirstOrDefaultAsync(r => r.EnrollmentId == enrollment.EnrollmentId);

            if (existing != null)
                throw new InvalidOperationException("You have already reviewed this course");

            var review = new Review
            {
                CourseId = dto.CourseId,
                StudentId = userId,
                EnrollmentId = enrollment.EnrollmentId,
                Rating = dto.Rating,
                Title = dto.Title != null ? Utils.SanitizeInput(dto.Title) : null,
                Body = Utils.SanitizeInput(dto.Body),
                IsVerifiedPurchase = enrollment.PaymentId.HasValue,
                Status = ReviewStatus.PUBLISHED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            await RecalculateCourseRatingAsync(dto.CourseId);

            return new
            {
                reviewId = review.ReviewId,
                courseId = review.CourseId,
                rating = review.Rating,
                title = review.Title,
                body = review.Body,
                isVerifiedPurchase = review.IsVerifiedPurchase,
                status = review.Status.ToString(),
                createdAt = review.CreatedAt
            };
        }

        // ─── Update Own Review (Student) ────────────────────────────
        public async Task<object> UpdateReviewAsync(int userId, int reviewId, UpdateReviewDto dto)
        {
            var review = await _db.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new InvalidOperationException("Review not found");

            if (review.StudentId != userId)
                throw new UnauthorizedAccessException("You can only edit your own review");

            if (dto.Rating.HasValue)
                review.Rating = dto.Rating.Value;

            if (dto.Title != null)
                review.Title = Utils.SanitizeInput(dto.Title);

            if (dto.Body != null)
                review.Body = Utils.SanitizeInput(dto.Body);

            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await RecalculateCourseRatingAsync(review.CourseId);

            return new
            {
                reviewId = review.ReviewId,
                rating = review.Rating,
                title = review.Title,
                body = review.Body,
                updatedAt = review.UpdatedAt
            };
        }

        // ─── Get Reviews for a Course (Public, paginated) ───────────
        public async Task<object> GetCourseReviewsAsync(int courseId, int? page, int? pageSize)
        {
            var query = _db.Reviews
                .Include(r => r.Student)
                .Where(r => r.CourseId == courseId && r.Status == ReviewStatus.PUBLISHED)
                .OrderByDescending(r => r.CreatedAt);

            var (pageNumber, size) = PaginationHelper.Normalize(page, pageSize);
            var total = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .Select(r => new
                {
                    reviewId = r.ReviewId,
                    rating = r.Rating,
                    title = r.Title,
                    body = r.Body,
                    isVerifiedPurchase = r.IsVerifiedPurchase,
                    helpfulCount = r.HelpfulCount,
                    createdAt = r.CreatedAt,
                    student = new
                    {
                        userId = r.Student.UserId,
                        fullName = r.Student.FullName
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

        // ─── Get One Review ────────────────────────────────────────
        public async Task<object> GetReviewAsync(int reviewId)
        {
            var review = await _db.Reviews
                .Include(r => r.Student)
                .Include(r => r.Course)
                    .ThenInclude(c => c.Academy)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new InvalidOperationException("Review not found");

            return new
            {
                reviewId = review.ReviewId,
                rating = review.Rating,
                title = review.Title,
                body = review.Body,
                status = review.Status.ToString(),
                isVerifiedPurchase = review.IsVerifiedPurchase,
                helpfulCount = review.HelpfulCount,
                createdAt = review.CreatedAt,
                updatedAt = review.UpdatedAt,
                student = new
                {
                    userId = review.Student.UserId,
                    fullName = review.Student.FullName
                },
                course = new
                {
                    courseId = review.Course.CourseId,
                    title = review.Course.Title,
                    slug = review.Course.Slug,
                    academy = new
                    {
                        academyId = review.Course.Academy.AcademyId,
                        name = review.Course.Academy.Name
                    }
                }
            };
        }

        // ─── My Reviews (Student) ──────────────────────────────────
        public async Task<List<object>> GetMyReviewsAsync(int userId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.Course)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    reviewId = r.ReviewId,
                    rating = r.Rating,
                    title = r.Title,
                    body = r.Body,
                    status = r.Status.ToString(),
                    helpfulCount = r.HelpfulCount,
                    createdAt = r.CreatedAt,
                    updatedAt = r.UpdatedAt,
                    course = new
                    {
                        courseId = r.Course.CourseId,
                        title = r.Course.Title,
                        slug = r.Course.Slug,
                        coverImageUrl = r.Course.CoverImageUrl
                    }
                })
                .ToListAsync();

            return reviews.Cast<object>().ToList();
        }

        // ─── Academy Reviews (Owner/Instructor/Moderator) ──────────
        public async Task<List<object>> GetAcademyReviewsAsync(
            int academyId, int? courseId, ReviewStatus? status)
        {
            var query = _db.Reviews
                .Include(r => r.Student)
                .Include(r => r.Course)
                .Where(r => r.Course.AcademyId == academyId);

            if (courseId.HasValue)
                query = query.Where(r => r.CourseId == courseId.Value);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    reviewId = r.ReviewId,
                    rating = r.Rating,
                    title = r.Title,
                    body = r.Body,
                    status = r.Status.ToString(),
                    isVerifiedPurchase = r.IsVerifiedPurchase,
                    helpfulCount = r.HelpfulCount,
                    createdAt = r.CreatedAt,
                    student = new
                    {
                        userId = r.Student.UserId,
                        fullName = r.Student.FullName,
                        email = r.Student.Email
                    },
                    course = new
                    {
                        courseId = r.Course.CourseId,
                        title = r.Course.Title
                    }
                })
                .ToListAsync();

            return reviews.Cast<object>().ToList();
        }

        // ─── Hide Review (Owner/Moderator, with reason) ────────────
        public async Task<object> HideReviewAsync(
            int reviewId, int requesterId, UserRole role, int? academyId, string reason)
        {
            var review = await _db.Reviews
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new InvalidOperationException("Review not found");

            // Only academy owner, superadmin, or moderator of the owning academy can hide
            var isPlatformAdmin = role == UserRole.SUPER_ADMIN;
            var isAcademyStaff = role == UserRole.ACADEMY_OWNER || role == UserRole.MODERATOR;

            if (!isPlatformAdmin && !isAcademyStaff)
                throw new UnauthorizedAccessException("Only academy owners, moderators, or superadmins can hide reviews");

            if (isAcademyStaff && review.Course.AcademyId != academyId)
                throw new UnauthorizedAccessException("Review belongs to another academy");

            if (review.Status == ReviewStatus.HIDDEN)
                throw new InvalidOperationException("Review is already hidden");

            review.Status = ReviewStatus.HIDDEN;
            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await RecalculateCourseRatingAsync(review.CourseId);

            return new
            {
                reviewId = review.ReviewId,
                status = review.Status.ToString(),
                reason = Utils.SanitizeInput(reason),
                message = "Review hidden"
            };
        }

        // ─── Unhide Review (Owner/Moderator) ──────────────────────
        public async Task<object> UnhideReviewAsync(
            int reviewId, int requesterId, UserRole role, int? academyId)
        {
            var review = await _db.Reviews
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new InvalidOperationException("Review not found");

            var isPlatformAdmin = role == UserRole.SUPER_ADMIN;
            var isAcademyStaff = role == UserRole.ACADEMY_OWNER || role == UserRole.MODERATOR;

            if (!isPlatformAdmin && !isAcademyStaff)
                throw new UnauthorizedAccessException("Only academy owners, moderators, or superadmins can unhide reviews");

            if (isAcademyStaff && review.Course.AcademyId != academyId)
                throw new UnauthorizedAccessException("Review belongs to another academy");

            if (review.Status != ReviewStatus.HIDDEN)
                throw new InvalidOperationException("Review is not hidden");

            review.Status = ReviewStatus.PUBLISHED;
            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await RecalculateCourseRatingAsync(review.CourseId);

            return new
            {
                reviewId = review.ReviewId,
                status = review.Status.ToString(),
                message = "Review restored"
            };
        }

        // ─── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Recomputes Course.AverageRating and Course.TotalReviews
        /// from all PUBLISHED reviews only.
        /// </summary>
        private async Task RecalculateCourseRatingAsync(int courseId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null) return;

            var stats = await _db.Reviews
                .Where(r => r.CourseId == courseId && r.Status == ReviewStatus.PUBLISHED)
                .GroupBy(r => r.CourseId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                course.TotalReviews = 0;
                course.AverageRating = null;
            }
            else
            {
                course.TotalReviews = stats.Count;
                course.AverageRating = (decimal)Math.Round(stats.Average, 2);
            }

            await _db.SaveChangesAsync();
        }
    }
}