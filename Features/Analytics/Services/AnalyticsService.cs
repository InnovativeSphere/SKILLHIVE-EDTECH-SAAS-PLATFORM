using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;

namespace SkillHive.Features.Analytics.Services
{
    public class AnalyticsService
    {
        private readonly AppDbContext _db;
        private readonly AnalyticsCache _cache;

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
        private const int DefaultRangeDays = 30;

        public AnalyticsService(AppDbContext db, AnalyticsCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // ─── SHARED HELPERS ─────────────────────────────────

       private static (DateTime from, DateTime to) ResolveDateRange(string? dateFrom, string? dateTo)
{
    var to = !string.IsNullOrWhiteSpace(dateTo)
        ? DateHelper.ParseEndDate(dateTo)
        : DateTime.UtcNow;

    var from = !string.IsNullOrWhiteSpace(dateFrom)
        ? DateHelper.ParseStartDate(dateFrom)
        : to.AddDays(-DefaultRangeDays);

    if (from > to)
        throw new InvalidOperationException("dateFrom must be before dateTo");

    return (from, to);
}

        // ═══════════════════════════════════════════════════
        // STUDENT ANALYTICS
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Student card-level summary — enrollments by status, certificates earned.
        /// </summary>
        public async Task<object> GetStudentOverviewAsync(int studentId)
        {
            var key = AnalyticsCache.BuildKey("student", "overview", studentId);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var enrollmentStats = await _db.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                int GetCount(EnrollmentStatus s) =>
                    enrollmentStats.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

                var certificatesEarned = await _db.Certificates
                    .CountAsync(c => c.StudentId == studentId
                                     && c.Status == CertificateStatus.ISSUED);

                var activeEnrollments = GetCount(EnrollmentStatus.ACTIVE);
                var completedEnrollments = GetCount(EnrollmentStatus.COMPLETED);

                // Sum lessons completed across all enrollments
                var lessonsCompleted = await _db.LessonProgresses
                    .CountAsync(lp => lp.Enrollment.StudentId == studentId && lp.IsCompleted);

                return new
                {
                    totalEnrollments = enrollmentStats.Sum(x => x.Count),
                    activeEnrollments,
                    completedEnrollments,
                    droppedEnrollments = GetCount(EnrollmentStatus.DROPPED),
                    certificatesEarned,
                    lessonsCompleted
                };
            });
        }

        /// <summary>
        /// Student's enrollments broken down by status + list with course details.
        /// </summary>
        public async Task<object> GetStudentEnrollmentsAsync(int studentId)
        {
            var key = AnalyticsCache.BuildKey("student", "enrollments", studentId);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var enrollments = await _db.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Academy)
                    .Where(e => e.StudentId == studentId)
                    .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
                    .Select(e => new
                    {
                        enrollmentId = e.EnrollmentId,
                        status = e.Status.ToString(),
                        progressPercentage = e.ProgressPercentage,
                        enrolledAt = e.EnrolledAt,
                        completedAt = e.CompletedAt,
                        lastAccessedAt = e.LastAccessedAt,
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
                                slug = e.Course.Academy.Slug
                            }
                        }
                    })
                    .ToListAsync();

                var byStatus = enrollments
                    .GroupBy(e => e.status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToList();

                return new
                {
                    byStatus,
                    items = enrollments
                };
            });
        }

        /// <summary>
        /// Certificates earned by the student.
        /// </summary>
        public async Task<object> GetStudentCertificatesAsync(int studentId)
        {
            var key = AnalyticsCache.BuildKey("student", "certificates", studentId);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var certificates = await _db.Certificates
                    .Include(c => c.Course)
                    .Include(c => c.Academy)
                    .Where(c => c.StudentId == studentId)
                    .OrderByDescending(c => c.IssuedAt)
                    .Select(c => new
                    {
                        certificateId = c.CertificateId,
                        verificationCode = c.VerificationCode,
                        status = c.Status.ToString(),
                        issuedAt = c.IssuedAt,
                        course = new
                        {
                            courseId = c.Course.CourseId,
                            title = c.Course.Title,
                            slug = c.Course.Slug
                        },
                        academy = new
                        {
                            academyId = c.Academy.AcademyId,
                            name = c.Academy.Name
                        }
                    })
                    .ToListAsync();

                return new
                {
                    total = certificates.Count,
                    issued = certificates.Count(c => c.status == "ISSUED"),
                    revoked = certificates.Count(c => c.status == "REVOKED"),
                    items = certificates
                };
            });
        }

        // ═══════════════════════════════════════════════════
        // ACADEMY (OWNER) ANALYTICS
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Card-level metrics for the academy dashboard.
        /// State metrics (courses, students) are not date-filtered;
        /// activity metrics (enrollments, revenue) respect the range.
        /// </summary>
        public async Task<object> GetAcademyOverviewAsync(
            int academyId, string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("academy", "overview", academyId, from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                // ─── Course counts (state, not date-filtered)
                var courseCounts = await _db.Courses
                    .Where(c => c.AcademyId == academyId)
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                int GetCourseCount(CourseStatus s) =>
                    courseCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

                // ─── Unique students
                var uniqueStudents = await _db.Enrollments
                    .Where(e => e.Course.AcademyId == academyId)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();

                // ─── Enrollments (date-filtered + total)
                var enrollmentsInRange = await _db.Enrollments
                    .CountAsync(e => e.Course.AcademyId == academyId
                                     && e.EnrolledAt >= from && e.EnrolledAt <= to);

                var totalEnrollments = await _db.Enrollments
                    .CountAsync(e => e.Course.AcademyId == academyId);

                var completedEnrollments = await _db.Enrollments
                    .CountAsync(e => e.Course.AcademyId == academyId
                                     && e.Status == EnrollmentStatus.COMPLETED);

                var completionRate = totalEnrollments > 0
                    ? Math.Round((double)completedEnrollments / totalEnrollments * 100, 2)
                    : 0;

                // ─── Revenue from course purchases
                var revenueTotal = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.COURSE_PURCHASE
                                && p.Status == PaymentStatus.SUCCESS
                                && p.Course!.AcademyId == academyId
                                && p.PaidAt >= from && p.PaidAt <= to)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                // ─── Average rating across academy courses
                var ratings = await _db.Courses
                    .Where(c => c.AcademyId == academyId && c.AverageRating != null)
                    .Select(c => c.AverageRating)
                    .ToListAsync();
                var averageRating = ratings.Count > 0
                    ? Math.Round(ratings.Average(r => r!.Value), 2)
                    : (decimal?)null;

                // ─── Certificates issued (date-filtered)
                var certificatesIssued = await _db.Certificates
                    .CountAsync(c => c.AcademyId == academyId
                                     && c.Status == CertificateStatus.ISSUED
                                     && c.IssuedAt >= from && c.IssuedAt <= to);

                return new
                {
                    period = new { from, to },
                    courses = new
                    {
                        total = courseCounts.Sum(x => x.Count),
                        published = GetCourseCount(CourseStatus.PUBLISHED),
                        draft = GetCourseCount(CourseStatus.DRAFT),
                        pendingReview = GetCourseCount(CourseStatus.PENDING_REVIEW),
                        archived = GetCourseCount(CourseStatus.ARCHIVED)
                    },
                    students = new
                    {
                        uniqueStudents
                    },
                    enrollments = new
                    {
                        total = totalEnrollments,
                        inRange = enrollmentsInRange,
                        completed = completedEnrollments,
                        completionRate
                    },
                    revenue = new
                    {
                        currency = "NGN",
                        totalInRange = revenueTotal
                    },
                    ratings = new
                    {
                        average = averageRating,
                        totalRatedCourses = ratings.Count
                    },
                    certificatesIssued
                };
            });
        }

        /// <summary>
        /// Per-course performance table for the academy.
        /// </summary>
        public async Task<object> GetAcademyCoursesAsync(
            int academyId, string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("academy", "courses", academyId, from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var courses = await _db.Courses
                    .Where(c => c.AcademyId == academyId)
                    .OrderByDescending(c => c.TotalEnrollments)
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
                        updatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                // Revenue per course in the range
                var revenueByCourse = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.COURSE_PURCHASE
                                && p.Status == PaymentStatus.SUCCESS
                                && p.Course!.AcademyId == academyId
                                && p.PaidAt >= from && p.PaidAt <= to)
                    .GroupBy(p => p.CourseId)
                    .Select(g => new { courseId = g.Key, revenue = g.Sum(p => p.Amount) })
                    .ToListAsync();

                // Recent enrollments per course in the range
                var enrollmentsByCourse = await _db.Enrollments
                    .Where(e => e.Course.AcademyId == academyId
                                && e.EnrolledAt >= from && e.EnrolledAt <= to)
                    .GroupBy(e => e.CourseId)
                    .Select(g => new { courseId = g.Key, count = g.Count() })
                    .ToListAsync();

                var result = courses.Select(c =>
                {
                    var revenue = revenueByCourse.FirstOrDefault(r => r.courseId == c.courseId)?.revenue ?? 0m;
                    var enrollmentsInRange = enrollmentsByCourse.FirstOrDefault(e => e.courseId == c.courseId)?.count ?? 0;

                    return new
                    {
                        c.courseId,
                        c.title,
                        c.slug,
                        c.status,
                        c.visibility,
                        c.isFree,
                        c.price,
                        c.totalLessons,
                        c.totalEnrollments,
                        c.averageRating,
                        c.totalReviews,
                        c.publishedAt,
                        c.updatedAt,
                        revenueInRange = revenue,
                        enrollmentsInRange
                    };
                }).ToList();

                return new
                {
                    period = new { from, to },
                    total = courses.Count,
                    items = result
                };
            });
        }

        /// <summary>
        /// Enrollment trend (daily) + recent enrollments.
        /// </summary>
        public async Task<object> GetAcademyEnrollmentsAsync(
            int academyId, string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("academy", "enrollments", academyId, from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                // Pull events in range
                var events = await _db.Enrollments
                    .Where(e => e.Course.AcademyId == academyId
                                && e.EnrolledAt >= from && e.EnrolledAt <= to)
                    .Select(e => new
                    {
                        e.EnrollmentId,
                        e.EnrolledAt,
                        e.Status,
                        e.ProgressPercentage,
                        courseTitle = e.Course.Title,
                        studentName = e.Student.FullName
                    })
                    .ToListAsync();

                var daily = events
                    .GroupBy(e => e.EnrolledAt.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        count = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                var recent = events
                    .OrderByDescending(e => e.EnrolledAt)
                    .Take(10)
                    .Select(e => new
                    {
                        enrollmentId = e.EnrollmentId,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status.ToString(),
                        progressPercentage = e.ProgressPercentage,
                        courseTitle = e.courseTitle,
                        studentName = e.studentName
                    })
                    .ToList();

                return new
                {
                    period = new { from, to },
                    totalInRange = events.Count,
                    daily,
                    recent
                };
            });
        }

        /// <summary>
        /// Course purchase revenue over time.
        /// </summary>
        public async Task<object> GetAcademyRevenueAsync(
            int academyId, string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("academy", "revenue", academyId, from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var payments = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.COURSE_PURCHASE
                                && p.Status == PaymentStatus.SUCCESS
                                && p.Course!.AcademyId == academyId
                                && p.PaidAt >= from && p.PaidAt <= to)
                    .Select(p => new { p.PaidAt, p.Amount, p.Currency })
                    .ToListAsync();

                var daily = payments
                    .Where(p => p.PaidAt.HasValue)
                    .GroupBy(p => p.PaidAt!.Value.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        revenue = g.Sum(x => x.Amount),
                        count = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                var total = payments.Sum(p => p.Amount);

                return new
                {
                    period = new { from, to },
                    currency = payments.FirstOrDefault()?.Currency ?? "NGN",
                    totalInRange = total,
                    transactionCount = payments.Count,
                    daily
                };
            });
        }

        /// <summary>
        /// Top 5 courses by enrollment + top 5 by revenue.
        /// </summary>
        public async Task<object> GetAcademyTopCoursesAsync(
            int academyId, string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("academy", "top-courses", academyId, from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var topByEnrollment = await _db.Courses
                    .Where(c => c.AcademyId == academyId)
                    .OrderByDescending(c => c.TotalEnrollments)
                    .Take(5)
                    .Select(c => new
                    {
                        courseId = c.CourseId,
                        title = c.Title,
                        slug = c.Slug,
                        totalEnrollments = c.TotalEnrollments,
                        averageRating = c.AverageRating
                    })
                    .ToListAsync();

                // Top by revenue in range
                var revenueGrouped = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.COURSE_PURCHASE
                                && p.Status == PaymentStatus.SUCCESS
                                && p.Course!.AcademyId == academyId
                                && p.PaidAt >= from && p.PaidAt <= to)
                    .GroupBy(p => p.CourseId)
                    .Select(g => new { courseId = g.Key, revenue = g.Sum(p => p.Amount) })
                    .OrderByDescending(x => x.revenue)
                    .Take(5)
                    .ToListAsync();

                var courseIds = revenueGrouped.Select(r => r.courseId).ToList();
                var courseTitles = await _db.Courses
                    .Where(c => courseIds.Contains(c.CourseId))
                    .Select(c => new { c.CourseId, c.Title, c.Slug })
                    .ToListAsync();

                var topByRevenue = revenueGrouped.Select(r =>
                {
                    var course = courseTitles.FirstOrDefault(c => c.CourseId == r.courseId);
                    return new
                    {
                        courseId = r.courseId,
                        title = course?.Title ?? "Unknown",
                        slug = course?.Slug ?? "",
                        revenue = r.revenue
                    };
                }).ToList();

                return new
                {
                    period = new { from, to },
                    topByEnrollment,
                    topByRevenue
                };
            });
        }

        // ═══════════════════════════════════════════════════
        // PLATFORM (SUPERADMIN) ANALYTICS
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Platform-wide card-level metrics.
        /// </summary>
        public async Task<object> GetPlatformOverviewAsync()
        {
            var key = AnalyticsCache.BuildKey("platform", "overview");

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var totalAcademies = await _db.Academies.CountAsync();
                var activeAcademies = await _db.Academies.CountAsync(a => a.IsActive);

                // Subscriptions by status
                var subscriptionsByStatus = await _db.Subscriptions
                    .GroupBy(s => s.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                int GetSubCount(SubscriptionStatus s) =>
                    subscriptionsByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

                var totalStudents = await _db.Users
                    .CountAsync(u => u.Role == UserRole.STUDENT);

                var totalCourses = await _db.Courses
                    .CountAsync(c => c.Status == CourseStatus.PUBLISHED);

                var totalEnrollments = await _db.Enrollments.CountAsync();

                var totalCertificates = await _db.Certificates
                    .CountAsync(c => c.Status == CertificateStatus.ISSUED);

                return new
                {
                    academies = new
                    {
                        total = totalAcademies,
                        active = activeAcademies,
                        trial = GetSubCount(SubscriptionStatus.TRIAL),
                        expired = GetSubCount(SubscriptionStatus.EXPIRED),
                        cancelled = GetSubCount(SubscriptionStatus.CANCELLED)
                    },
                    students = new { total = totalStudents },
                    courses = new { published = totalCourses },
                    enrollments = new { total = totalEnrollments },
                    certificates = new { issued = totalCertificates }
                };
            });
        }

        /// <summary>
        /// Platform revenue from subscriptions (MRR + total in range).
        /// </summary>
        public async Task<object> GetPlatformRevenueAsync(string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("platform", "revenue", from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                // Subscription payments in range
                var payments = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.SUBSCRIPTION
                                && p.Status == PaymentStatus.SUCCESS
                                && p.PaidAt >= from && p.PaidAt <= to)
                    .Select(p => new { p.PaidAt, p.Amount, p.Currency })
                    .ToListAsync();

                var daily = payments
                    .Where(p => p.PaidAt.HasValue)
                    .GroupBy(p => p.PaidAt!.Value.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        revenue = g.Sum(x => x.Amount),
                        count = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                var totalInRange = payments.Sum(p => p.Amount);

                // MRR — normalize quarterly and annual to monthly
                var activeSubs = await _db.Subscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.Status == SubscriptionStatus.ACTIVE
                                || s.Status == SubscriptionStatus.GRACE)
                    .ToListAsync();

                decimal mrr = 0m;
                foreach (var sub in activeSubs)
                {
                    mrr += sub.Plan.Interval switch
                    {
                        SubscriptionInterval.MONTHLY => sub.Plan.Price,
                        SubscriptionInterval.QUARTERLY => sub.Plan.Price / 3m,
                        SubscriptionInterval.ANNUAL => sub.Plan.Price / 12m,
                        _ => 0m
                    };
                }

                return new
                {
                    period = new { from, to },
                    currency = "NGN",
                    totalInRange,
                    transactionCount = payments.Count,
                    mrr = Math.Round(mrr, 2),
                    activeSubscriptions = activeSubs.Count,
                    daily
                };
            });
        }

        /// <summary>
        /// Academies broken down by plan, by status, by signup month.
        /// </summary>
        public async Task<object> GetPlatformAcademiesAsync()
        {
            var key = AnalyticsCache.BuildKey("platform", "academies");

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                // By plan
                var byPlan = await _db.Subscriptions
                    .Include(s => s.Plan)
                    .GroupBy(s => s.Plan.Name)
                    .Select(g => new
                    {
                        planName = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                // By subscription status
                var byStatus = await _db.Subscriptions
                    .GroupBy(s => s.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToListAsync();

                // By signup month (academy created)
                var academies = await _db.Academies
                    .Select(a => a.CreatedAt)
                    .ToListAsync();

                var bySignupMonth = academies
                    .GroupBy(d => new { d.Year, d.Month })
                    .Select(g => new
                    {
                        month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        count = g.Count()
                    })
                    .OrderBy(x => x.month)
                    .ToList();

                return new
                {
                    byPlan,
                    byStatus = byStatus.Select(s => new
                    {
                        status = s.status.ToString(),
                        count = s.count
                    }),
                    bySignupMonth
                };
            });
        }

        /// <summary>
        /// Signups over time — new academies + new students in range.
        /// </summary>
        public async Task<object> GetPlatformSignupsAsync(string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("platform", "signups", from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                var academyDates = await _db.Academies
                    .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
                    .Select(a => a.CreatedAt)
                    .ToListAsync();

                var studentDates = await _db.Users
                    .Where(u => u.Role == UserRole.STUDENT
                                && u.CreatedAt >= from && u.CreatedAt <= to)
                    .Select(u => u.CreatedAt)
                    .ToListAsync();

                var academiesDaily = academyDates
                    .GroupBy(d => d.Date)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
                    .OrderBy(x => x.date)
                    .ToList();

                var studentsDaily = studentDates
                    .GroupBy(d => d.Date)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
                    .OrderBy(x => x.date)
                    .ToList();

                return new
                {
                    period = new { from, to },
                    totalNewAcademies = academyDates.Count,
                    totalNewStudents = studentDates.Count,
                    academiesDaily,
                    studentsDaily
                };
            });
        }

        /// <summary>
        /// Top academies by subscription revenue + by student count.
        /// </summary>
        public async Task<object> GetPlatformTopAcademiesAsync(string? dateFrom, string? dateTo)
        {
            var (from, to) = ResolveDateRange(dateFrom, dateTo);
            var key = AnalyticsCache.BuildKey("platform", "top-academies", from.Date, to.Date);

            return await _cache.GetOrSetAsync(key, CacheTtl, async () =>
            {
                // Top by subscription revenue in range
                var revenueByAcademy = await _db.PaymentTransactions
                    .Where(p => p.Purpose == PaymentPurpose.SUBSCRIPTION
                                && p.Status == PaymentStatus.SUCCESS
                                && p.PaidAt >= from && p.PaidAt <= to
                                && p.AcademyId != null)
                    .GroupBy(p => p.AcademyId)
                    .Select(g => new { academyId = g.Key, revenue = g.Sum(p => p.Amount) })
                    .OrderByDescending(x => x.revenue)
                    .Take(5)
                    .ToListAsync();

                var academyIds = revenueByAcademy.Select(r => r.academyId).ToList();
                var academies = await _db.Academies
                    .Where(a => academyIds.Contains(a.AcademyId))
                    .Select(a => new { a.AcademyId, a.Name, a.Slug })
                    .ToListAsync();

                var topByRevenue = revenueByAcademy.Select(r =>
                {
                    var a = academies.FirstOrDefault(x => x.AcademyId == r.academyId);
                    return new
                    {
                        academyId = r.academyId,
                        name = a?.Name ?? "Unknown",
                        slug = a?.Slug ?? "",
                        revenue = r.revenue
                    };
                }).ToList();

                // Top by student count
                var studentsByAcademy = await _db.Enrollments
                    .GroupBy(e => e.Course.AcademyId)
                    .Select(g => new
                    {
                        academyId = g.Key,
                        studentCount = g.Select(e => e.StudentId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.studentCount)
                    .Take(5)
                    .ToListAsync();

                var topStudentAcademyIds = studentsByAcademy.Select(s => s.academyId).ToList();
                var studentAcademies = await _db.Academies
                    .Where(a => topStudentAcademyIds.Contains(a.AcademyId))
                    .Select(a => new { a.AcademyId, a.Name, a.Slug })
                    .ToListAsync();

                var topByStudents = studentsByAcademy.Select(s =>
                {
                    var a = studentAcademies.FirstOrDefault(x => x.AcademyId == s.academyId);
                    return new
                    {
                        academyId = s.academyId,
                        name = a?.Name ?? "Unknown",
                        slug = a?.Slug ?? "",
                        studentCount = s.studentCount
                    };
                }).ToList();

                return new
                {
                    period = new { from, to },
                    topByRevenue,
                    topByStudents
                };
            });
        }
    }
}