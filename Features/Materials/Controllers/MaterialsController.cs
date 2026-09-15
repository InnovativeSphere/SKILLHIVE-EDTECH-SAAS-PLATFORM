using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Materials.DTOs;
using SkillHive.Features.Materials.Services;

namespace SkillHive.Features.Materials.Controllers
{
    [ApiController]
    [Route("api")]
    public class MaterialsController : ControllerBase
    {
        private readonly MaterialService _materialService;

        public MaterialsController(MaterialService materialService)
        {
            _materialService = materialService;
        }

        // ─── Upload file to Cloudinary ─────────────────────────────
        [HttpPost("materials/upload")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        [RequestSizeLimit(50_000_000)] // 50 MB
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return ApiResponse.BadRequest("No file provided");

                using var stream = file.OpenReadStream();
                var result = await _materialService.UploadFileAsync(stream, file.FileName);
                return ApiResponse.Created(result, "File uploaded");
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

        // ─── List materials for a lesson (public/staff) ────────────
        [HttpGet("lessons/{lessonId:int}/materials")]
        public async Task<IActionResult> GetForLesson(int lessonId)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                var roleStr = JwtHelper.GetRole(User);
                var isStaff = roleStr == UserRole.ACADEMY_OWNER.ToString()
                              || roleStr == UserRole.INSTRUCTOR.ToString()
                              || roleStr == UserRole.MODERATOR.ToString();

                var result = await _materialService.GetForLessonAsync(lessonId, isStaff, academyId);
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

        // ─── Create material record ────────────────────────────────
        [HttpPost("lessons/{lessonId:int}/materials")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Create(int lessonId, [FromBody] CreateMaterialDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _materialService.CreateAsync(userId, role, academyId, lessonId, dto);
                return ApiResponse.Created(result, "Material created");
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

        // ─── Update material ───────────────────────────────────────
        [HttpPatch("materials/{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMaterialDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _materialService.UpdateAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Material updated");
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

        // ─── Deactivate material ───────────────────────────────────
        [HttpDelete("materials/{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _materialService.DeactivateAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Material deactivated");
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