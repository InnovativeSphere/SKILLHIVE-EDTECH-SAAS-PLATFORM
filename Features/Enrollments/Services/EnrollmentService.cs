using Microsoft.EntityFrameworkCore;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Certificates.Services;
using SkillHive.Features.Enrollments.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Enrollments.Services
{
    public class EnrollmentService
    {
        private readonly AppDbContext _db;
        private readonly CertificateService _certificateService;

        public EnrollmentService(AppDbContext db, CertificateService certificateService)
        {
            _db = db;
            _certificateService = certificateService;
        }

        // ─── Enroll in a Course (Student) ───────────────────────────
        public async Task<object> EnrollAsync(int userId, UserRole role, EnrollDto dto)
        {
            if (role != UserRole.STUDENT)
                throw new UnauthorizedAccessException("Only students can enroll in courses");

            var course = await _db.Courses
                .Include(c => c.Academy)
                .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

            if (course == null)
                throw new InvalidOperationException("Course not found");

            if (course.Status != CourseStatus.PUBLISHED)
                throw new InvalidOperationException("Course is not available for enrollment");

            if (course.Visibility == CourseVisibility.PRIVATE)
                throw new InvalidOperationException("Course is not available for enrollment");

            if (!course.Academy.IsActive)
                throw new InvalidOperationException("Course is not available for enrollment");

            if (!course.IsFree)
                throw new InvalidOperationException("This is a paid course. Payment flow is not yet available.");

            // Check for existing enrollment
            var existing = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == dto.CourseId);

            Enrollment enrollment;

            if (existing != null)
            {
                if (existing.Status == EnrollmentStatus.ACTIVE || existing.Status == EnrollmentStatus.COMPLETED)
                    throw new InvalidOperationException("You are already enrolled in this course");

                // Reactivate dropped/expired enrollment — preserve LessonProgress
                existing.Status = EnrollmentStatus.ACTIVE;
                existing.EnrolledAt = DateTime.UtcNow;
                existing.LastAccessedAt = DateTime.UtcNow;
                enrollment = existing;
            }
            else
            {
                enrollment = new Enrollment
                {
                    StudentId = userId,
                    CourseId = dto.CourseId,
                    Status = EnrollmentStatus.ACTIVE,
                    EnrolledAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    ProgressPercentage = 0
                };
                _db.Enrollments.Add(enrollment);
            }

            // Auto-follow the academy (soft-follow pattern)
            var alreadyFollowing = await _db.StudentAcademyFollows
                .AnyAsync(f => f.StudentId == userId && f.AcademyId == course.AcademyId);

            if (!alreadyFollowing)
            {
                _db.StudentAcademyFollows.Add(new StudentAcademyFollow
                {
                    StudentId = userId,
                    AcademyId = course.AcademyId,
                    NotifyOnNewCourse = true,
                    FollowedAt = DateTime.UtcNow
                });
            }

            // Bump course counter only for brand-new enrollments
            if (existing == null)
                course.TotalEnrollments += 1;

            await _db.SaveChangesAsync();

            return new
            {
                enrollmentId = enrollment.EnrollmentId,
                studentId = enrollment.StudentId,
                courseId = enrollment.CourseId,
                status = enrollment.Status.ToString(),
                enrolledAt = enrollment.EnrolledAt,
                progressPercentage = enrollment.ProgressPercentage
            };
        }

        // ─── Mark Lesson Complete (Student) ─────────────────────────
        public async Task<object> MarkLessonCompleteAsync(
            int userId, UserRole role, MarkLessonCompleteDto dto)
        {
            if (role != UserRole.STUDENT)
                throw new UnauthorizedAccessException("Only students can mark lessons complete");

            var lesson = await _db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == dto.LessonId && l.IsActive);

            if (lesson == null)
                throw new InvalidOperationException("Lesson not found");

            var enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == userId
                                          && e.CourseId == lesson.CourseId
                                          && e.Status == EnrollmentStatus.ACTIVE);

            if (enrollment == null)
                throw new InvalidOperationException("You are not actively enrolled in this course");

            // Upsert progress row
            var progress = await _db.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.EnrollmentId == enrollment.EnrollmentId
                                           && lp.LessonId == lesson.LessonId);

            if (progress == null)
            {
                _db.LessonProgresses.Add(new LessonProgress
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    LessonId = lesson.LessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            enrollment.LastAccessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Recalculate progress + completion (may trigger certificate)
            var update = await RecalculateProgressAsync(enrollment.EnrollmentId);

            return new
            {
                enrollmentId = enrollment.EnrollmentId,
                lessonId = lesson.LessonId,
                progressPercentage = update.ProgressPercentage,
                status = update.Status,
                completedAt = update.CompletedAt
            };
        }

        // ─── Get My Enrollments (Student) ───────────────────────────
        public async Task<List<object>> GetMyEnrollmentsAsync(int userId, UserRole role)
        {
            if (role != UserRole.STUDENT)
                throw new UnauthorizedAccessException("Only students can view personal enrollments");

            var enrollments = await _db.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Academy)
                .Where(e => e.StudentId == userId)
                .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
                .Select(e => new
                {
                    enrollmentId = e.EnrollmentId,
                    status = e.Status.ToString(),
                    progressPercentage = e.ProgressPercentage,
                    enrolledAt = e.EnrolledAt,
                    completedAt = e.CompletedAt,
                    lastAccessedAt = e.LastAccessedAt,
                    certificateIssuedAt = e.CertificateIssuedAt,
                    course = new
                    {
                        courseId = e.Course.CourseId,
                        title = e.Course.Title,
                        slug = e.Course.Slug,
                        coverImageUrl = e.Course.CoverImageUrl,
                        isFree = e.Course.IsFree,
                        totalLessons = e.Course.TotalLessons,
                        academy = new
                        {
                            academyId = e.Course.Academy.AcademyId,
                            name = e.Course.Academy.Name,
                            slug = e.Course.Academy.Slug,
                            logoUrl = e.Course.Academy.LogoUrl
                        }
                    }
                })
                .ToListAsync();

            return enrollments.Cast<object>().ToList();
        }

        // ─── Get Single Enrollment (with lesson progress) ──────────
        public async Task<object> GetEnrollmentAsync(int userId, UserRole role, int? academyId, int enrollmentId)
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Academy)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            // Permission: student can only see own; owner/instructor can see own academy's
            if (role == UserRole.STUDENT)
            {
                if (enrollment.StudentId != userId)
                    throw new UnauthorizedAccessException("You can only view your own enrollment");
            }
            else if (role == UserRole.ACADEMY_OWNER || role == UserRole.INSTRUCTOR)
            {
                if (enrollment.Course.AcademyId != academyId)
                    throw new UnauthorizedAccessException("Enrollment belongs to another academy");
            }
            else
            {
                throw new UnauthorizedAccessException("You do not have permission to view enrollments");
            }

            var lessonProgress = await _db.LessonProgresses
                .Where(lp => lp.EnrollmentId == enrollmentId)
                .Select(lp => new
                {
                    lessonId = lp.LessonId,
                    isCompleted = lp.IsCompleted,
                    completedAt = lp.CompletedAt
                })
                .ToListAsync();

            return new
            {
                enrollmentId = enrollment.EnrollmentId,
                studentId = enrollment.StudentId,
                status = enrollment.Status.ToString(),
                progressPercentage = enrollment.ProgressPercentage,
                enrolledAt = enrollment.EnrolledAt,
                completedAt = enrollment.CompletedAt,
                lastAccessedAt = enrollment.LastAccessedAt,
                certificateIssuedAt = enrollment.CertificateIssuedAt,
                course = new
                {
                    courseId = enrollment.Course.CourseId,
                    title = enrollment.Course.Title,
                    slug = enrollment.Course.Slug,
                    coverImageUrl = enrollment.Course.CoverImageUrl,
                    totalLessons = enrollment.Course.TotalLessons,
                    academy = new
                    {
                        academyId = enrollment.Course.Academy.AcademyId,
                        name = enrollment.Course.Academy.Name,
                        slug = enrollment.Course.Academy.Slug
                    }
                },
                lessonProgress
            };
        }

        // ─── List Academy Enrollments (Owner/Instructor) ────────────
        public async Task<List<object>> ListAcademyEnrollmentsAsync(
            int userId, UserRole role, int? academyId, int? courseId)
        {
            if (role != UserRole.ACADEMY_OWNER && role != UserRole.INSTRUCTOR && role != UserRole.MODERATOR)
                throw new UnauthorizedAccessException("Only academy staff can list enrollments");

            if (academyId == null)
                throw new InvalidOperationException("You do not belong to an academy");

            var query = _db.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .Where(e => e.Course.AcademyId == academyId);

            if (courseId.HasValue)
                query = query.Where(e => e.CourseId == courseId.Value);

            var enrollments = await query
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    enrollmentId = e.EnrollmentId,
                    status = e.Status.ToString(),
                    progressPercentage = e.ProgressPercentage,
                    enrolledAt = e.EnrolledAt,
                    completedAt = e.CompletedAt,
                    student = new
                    {
                        userId = e.Student.UserId,
                        fullName = e.Student.FullName,
                        email = e.Student.Email
                    },
                    course = new
                    {
                        courseId = e.Course.CourseId,
                        title = e.Course.Title
                    }
                })
                .ToListAsync();

            return enrollments.Cast<object>().ToList();
        }

        // ─── Drop Enrollment (Student) ──────────────────────────────
        public async Task<object> DropEnrollmentAsync(int userId, int enrollmentId)
        {
            var enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            if (enrollment.StudentId != userId)
                throw new UnauthorizedAccessException("You can only drop your own enrollment");

            if (enrollment.Status == EnrollmentStatus.DROPPED)
                throw new InvalidOperationException("Enrollment is already dropped");

            if (enrollment.Status == EnrollmentStatus.COMPLETED)
                throw new InvalidOperationException("Cannot drop a completed course");

            enrollment.Status = EnrollmentStatus.DROPPED;
            await _db.SaveChangesAsync();

            return new
            {
                enrollmentId = enrollment.EnrollmentId,
                status = enrollment.Status.ToString(),
                message = "Enrollment dropped. Your progress is preserved if you return."
            };
        }

        // ─── Unfollow Academy (Student) ─────────────────────────────
        public async Task<object> UnfollowAcademyAsync(int userId, UserRole role, UnfollowAcademyDto dto)
        {
            if (role != UserRole.STUDENT)
                throw new UnauthorizedAccessException("Only students can unfollow academies");

            var follow = await _db.StudentAcademyFollows
                .FirstOrDefaultAsync(f => f.StudentId == userId && f.AcademyId == dto.AcademyId);

            if (follow == null)
                throw new InvalidOperationException("You are not following this academy");

            _db.StudentAcademyFollows.Remove(follow);
            await _db.SaveChangesAsync();

            return new { academyId = dto.AcademyId, message = "Unfollowed" };
        }

        // ─── Get My Followed Academies (Student) ────────────────────
        public async Task<List<object>> GetMyFollowedAcademiesAsync(int userId)
        {
            var follows = await _db.StudentAcademyFollows
                .Include(f => f.Academy)
                .Where(f => f.StudentId == userId)
                .Select(f => new
                {
                    academyId = f.Academy.AcademyId,
                    name = f.Academy.Name,
                    slug = f.Academy.Slug,
                    logoUrl = f.Academy.LogoUrl,
                    notifyOnNewCourse = f.NotifyOnNewCourse,
                    followedAt = f.FollowedAt
                })
                .ToListAsync();

            return follows.Cast<object>().ToList();
        }

        // ─── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Recalculates an enrollment's progress percentage and completion status.
        /// Completion = all active lessons done AND all required quizzes passed.
        /// On completion, triggers certificate generation.
        /// </summary>
        private async Task<(int ProgressPercentage, string Status, DateTime? CompletedAt)> RecalculateProgressAsync(int enrollmentId)
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            // Count active lessons in the course
            var totalLessons = await _db.Lessons
                .CountAsync(l => l.CourseId == enrollment.CourseId && l.IsActive);

            if (totalLessons == 0)
            {
                enrollment.ProgressPercentage = 0;
                await _db.SaveChangesAsync();
                return (0, enrollment.Status.ToString(), enrollment.CompletedAt);
            }

            var completedLessons = await _db.LessonProgresses
                .CountAsync(lp => lp.EnrollmentId == enrollmentId && lp.IsCompleted);

            var percentage = Math.Min(100, (int)Math.Round((double)completedLessons / totalLessons * 100));
            enrollment.ProgressPercentage = percentage;

            // Check quiz requirements — quizzes on the course or any of its lessons
            var lessonIds = await _db.Lessons
                .Where(l => l.CourseId == enrollment.CourseId && l.IsActive)
                .Select(l => l.LessonId)
                .ToListAsync();

            var requiredQuizIds = await _db.Quizzes
                .Where(q => q.CourseId == enrollment.CourseId
                            || (q.LessonId != null && lessonIds.Contains(q.LessonId.Value)))
                .Select(q => q.QuizId)
                .ToListAsync();

            var allQuizzesPassed = true;

            if (requiredQuizIds.Count > 0)
            {
                var passedQuizIds = await _db.QuizAttempts
                    .Where(a => a.StudentId == enrollment.StudentId
                                && a.Passed
                                && requiredQuizIds.Contains(a.QuizId))
                    .Select(a => a.QuizId)
                    .Distinct()
                    .ToListAsync();

                allQuizzesPassed = requiredQuizIds.All(id => passedQuizIds.Contains(id));
            }

            // Completion: all lessons done + all required quizzes passed
            var shouldComplete = percentage == 100 && allQuizzesPassed
                                 && enrollment.Status == EnrollmentStatus.ACTIVE;

            if (shouldComplete)
            {
                enrollment.Status = EnrollmentStatus.COMPLETED;
                enrollment.CompletedAt = DateTime.UtcNow;

                // Save completion FIRST so certificate service can read the updated status
                await _db.SaveChangesAsync();

                // Then generate certificate (best-effort; failure doesn't block enrollment)
                try
                {
                    await _certificateService.GenerateForEnrollmentAsync(enrollment.EnrollmentId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Certificate generation failed for enrollment {enrollment.EnrollmentId}: {ex.Message}");
                }

                return (enrollment.ProgressPercentage, enrollment.Status.ToString(), enrollment.CompletedAt);
            }

            await _db.SaveChangesAsync();
            return (enrollment.ProgressPercentage, enrollment.Status.ToString(), enrollment.CompletedAt);
        }
    }
}