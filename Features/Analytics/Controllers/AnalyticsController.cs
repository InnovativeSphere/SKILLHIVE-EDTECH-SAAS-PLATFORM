using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Analytics.Services;

namespace SkillHive.Features.Analytics.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // ═══════════════════════════════════════════════════
        // STUDENT
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Student dashboard card-level metrics:
        /// total enrollments, active/completed/dropped, certificates, lessons completed.
        /// </summary>
        [HttpGet("student/overview")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetStudentOverview()
        {
            try
            {
                var studentId = JwtHelper.GetUserId(User);
                var result = await _analyticsService.GetStudentOverviewAsync(studentId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Student's enrollments grouped by status + list with course details.
        /// </summary>
        [HttpGet("student/enrollments")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetStudentEnrollments()
        {
            try
            {
                var studentId = JwtHelper.GetUserId(User);
                var result = await _analyticsService.GetStudentEnrollmentsAsync(studentId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Student's earned certificates with course + academy details.
        /// </summary>
        [HttpGet("student/certificates")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetStudentCertificates()
        {
            try
            {
                var studentId = JwtHelper.GetUserId(User);
                var result = await _analyticsService.GetStudentCertificatesAsync(studentId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ═══════════════════════════════════════════════════
        // ACADEMY (OWNER)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Academy card-level dashboard metrics.
        /// Academy scope is derived from the owner's JWT — no query param.
        /// </summary>
        [HttpGet("academy/overview")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyOverview(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _analyticsService.GetAcademyOverviewAsync(
                    academyId.Value, dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Per-course performance table for the academy.
        /// </summary>
        [HttpGet("academy/courses")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyCourses(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _analyticsService.GetAcademyCoursesAsync(
                    academyId.Value, dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Enrollment trends (daily) + recent enrollments for the academy.
        /// </summary>
        [HttpGet("academy/enrollments")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyEnrollments(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _analyticsService.GetAcademyEnrollmentsAsync(
                    academyId.Value, dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Course purchase revenue over time.
        /// </summary>
        [HttpGet("academy/revenue")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> GetAcademyRevenue(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _analyticsService.GetAcademyRevenueAsync(
                    academyId.Value, dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Top 5 courses by enrollment + top 5 by revenue.
        /// </summary>
        [HttpGet("academy/top-courses")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyTopCourses(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _analyticsService.GetAcademyTopCoursesAsync(
                    academyId.Value, dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ═══════════════════════════════════════════════════
        // PLATFORM (SUPERADMIN)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Platform-wide card-level metrics. Snapshot in time — no date filter.
        /// </summary>
        [HttpGet("platform/overview")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlatformOverview()
        {
            try
            {
                var result = await _analyticsService.GetPlatformOverviewAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Platform subscription revenue (MRR + total in range).
        /// </summary>
        [HttpGet("platform/revenue")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlatformRevenue(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var result = await _analyticsService.GetPlatformRevenueAsync(dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Academies broken down by plan, by subscription status, and by signup month.
        /// Snapshot in time — no date filter.
        /// </summary>
        [HttpGet("platform/academies")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlatformAcademies()
        {
            try
            {
                var result = await _analyticsService.GetPlatformAcademiesAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Signups over time — new academies + new students in range.
        /// </summary>
        [HttpGet("platform/signups")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlatformSignups(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var result = await _analyticsService.GetPlatformSignupsAsync(dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Top 5 academies by subscription revenue + top 5 by student count.
        /// </summary>
        [HttpGet("platform/top-academies")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlatformTopAcademies(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo)
        {
            try
            {
                var result = await _analyticsService.GetPlatformTopAcademiesAsync(dateFrom, dateTo);
                return ApiResponse.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }
    }
}