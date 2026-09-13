using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Courses.DTOs;
using SkillHive.Features.Courses.Services;

namespace SkillHive.Features.Courses.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly CourseService _courseService;

        public CoursesController(CourseService courseService)
        {
            _courseService = courseService;
        }

        // ─── Public Browse ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Browse(
            [FromQuery] int? categoryId,
            [FromQuery] int? professionId,
            [FromQuery] int? academyId,
            [FromQuery] CourseLevel? level,
            [FromQuery] bool? isFree,
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var result = await _courseService.BrowseCoursesAsync(
                    categoryId, professionId, academyId, level, isFree, search, page, pageSize);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                var roleStr = JwtHelper.GetRole(User);
                var isStaff = roleStr == UserRole.ACADEMY_OWNER.ToString()
                              || roleStr == UserRole.INSTRUCTOR.ToString()
                              || roleStr == UserRole.MODERATOR.ToString();

                var result = await _courseService.GetCourseAsync(slug, academyId, isStaff);
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

        // ─── Academy Dashboard Listing ─────────────────────────────
        [HttpGet("academy/mine")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyCourses()
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                var result = await _courseService.GetAcademyCoursesAsync(academyId);
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

        // ─── Create ───────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.CreateCourseAsync(userId, role, academyId, dto);
                return ApiResponse.Created(result, "Course created");
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

        // ─── Update ───────────────────────────────────────────────
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.UpdateCourseAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Course updated");
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

        // ─── Lifecycle: Submit for Review ─────────────────────────
        [HttpPatch("{id:int}/submit")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Submit(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.SubmitForReviewAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Course submitted for review");
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

        // ─── Lifecycle: Approve ───────────────────────────────────
        [HttpPatch("{id:int}/approve")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.ApproveCourseAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Course approved");
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

        // ─── Lifecycle: Reject ────────────────────────────────────
        [HttpPatch("{id:int}/reject")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectCourseDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.RejectCourseAsync(userId, role, academyId, id, dto.Reason);
                return ApiResponse.Success(result, "Course rejected");
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

        // ─── Lifecycle: Archive ───────────────────────────────────
        [HttpPatch("{id:int}/archive")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _courseService.ArchiveCourseAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Course archived");
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