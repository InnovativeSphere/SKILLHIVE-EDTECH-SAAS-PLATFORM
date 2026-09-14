using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Lessons.DTOs;
using SkillHive.Features.Lessons.Services;

namespace SkillHive.Features.Lessons.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    public class LessonsController : ControllerBase
    {
        private readonly LessonService _lessonService;

        public LessonsController(LessonService lessonService)
        {
            _lessonService = lessonService;
        }

        // ─── List lessons for a course (public/staff) ─────────────
        [HttpGet("/api/courses/{courseSlug}/lessons")]
        public async Task<IActionResult> GetCourseLessons(string courseSlug)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                var roleStr = JwtHelper.GetRole(User);
                var isStaff = roleStr == UserRole.ACADEMY_OWNER.ToString()
                              || roleStr == UserRole.INSTRUCTOR.ToString()
                              || roleStr == UserRole.MODERATOR.ToString();

                var result = await _lessonService.GetCourseLessonsAsync(courseSlug, isStaff, academyId);
                return ApiResponse.Success(result);
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

        // ─── Get single lesson (content exposure rules) ───────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLesson(int id)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                var roleStr = JwtHelper.GetRole(User);
                var isStaff = roleStr == UserRole.ACADEMY_OWNER.ToString()
                              || roleStr == UserRole.INSTRUCTOR.ToString()
                              || roleStr == UserRole.MODERATOR.ToString();

                var result = await _lessonService.GetLessonAsync(id, isStaff, academyId);
                return ApiResponse.Success(result);
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

        // ─── Create lesson under a course ─────────────────────────
        [HttpPost("/api/courses/{courseId:int}/lessons")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Create(int courseId, [FromBody] CreateLessonDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _lessonService.CreateLessonAsync(userId, role, academyId, courseId, dto);
                return ApiResponse.Created(result, "Lesson created");
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

        // ─── Update lesson ────────────────────────────────────────
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _lessonService.UpdateLessonAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Lesson updated");
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

        // ─── Reorder lesson ───────────────────────────────────────
        [HttpPatch("{id:int}/reorder")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Reorder(int id, [FromBody] ReorderLessonDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _lessonService.ReorderLessonAsync(userId, role, academyId, id, dto.NewOrder);
                return ApiResponse.Success(result, "Lesson reordered");
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

        // ─── Deactivate lesson (soft delete) ──────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _lessonService.DeactivateLessonAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Lesson deactivated");
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

        // ─── Helpers ──────────────────────────────────────────────
        private UserRole ParseRole()
        {
            var roleStr = JwtHelper.GetRole(User);
            return Enum.TryParse<UserRole>(roleStr, out var role) ? role : UserRole.STUDENT;
        }
    }
}