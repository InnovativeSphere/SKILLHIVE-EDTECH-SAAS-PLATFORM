using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Enrollments.DTOs;
using SkillHive.Features.Enrollments.Services;

namespace SkillHive.Features.Enrollments.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly EnrollmentService _enrollmentService;

        public EnrollmentsController(EnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        // ─── Enroll in a course (Student) ──────────────────────────
        [HttpPost]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Enroll([FromBody] EnrollDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();

                var result = await _enrollmentService.EnrollAsync(userId, role, dto);
                return ApiResponse.Created(result, "Enrolled successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
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

        // ─── Mark lesson complete (Student) ────────────────────────
        [HttpPost("lessons/complete")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> MarkLessonComplete([FromBody] MarkLessonCompleteDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();

                var result = await _enrollmentService.MarkLessonCompleteAsync(userId, role, dto);
                return ApiResponse.Success(result, "Lesson marked complete");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
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

        // ─── My enrollments (Student) ──────────────────────────────
        [HttpGet("me")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetMyEnrollments()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();

                var result = await _enrollmentService.GetMyEnrollmentsAsync(userId, role);
                return ApiResponse.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── My followed academies (Student) ──────────────────────
        [HttpGet("follows")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetMyFollowedAcademies()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _enrollmentService.GetMyFollowedAcademiesAsync(userId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Unfollow an academy (Student) ────────────────────────
        [HttpDelete("follows")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> UnfollowAcademy([FromBody] UnfollowAcademyDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();

                var result = await _enrollmentService.UnfollowAcademyAsync(userId, role, dto);
                return ApiResponse.Success(result, "Unfollowed");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
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

        // ─── List academy enrollments (Owner/Instructor/Moderator) ─
        [HttpGet("academy")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> ListAcademyEnrollments([FromQuery] int? courseId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _enrollmentService.ListAcademyEnrollmentsAsync(userId, role, academyId, courseId);
                return ApiResponse.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
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

        // ─── Get one enrollment ────────────────────────────────────
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetEnrollment(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _enrollmentService.GetEnrollmentAsync(userId, role, academyId, id);
                return ApiResponse.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Drop enrollment (Student) ─────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> DropEnrollment(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);

                var result = await _enrollmentService.DropEnrollmentAsync(userId, id);
                return ApiResponse.Success(result, "Enrollment dropped");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
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

        // ─── Helpers ───────────────────────────────────────────────
        private UserRole ParseRole()
        {
            var roleStr = JwtHelper.GetRole(User);
            return Enum.TryParse<UserRole>(roleStr, out var role) ? role : UserRole.STUDENT;
        }
    }
}