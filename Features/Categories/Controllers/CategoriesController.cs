using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Categories.DTOs;
using SkillHive.Features.Categories.Services;

namespace SkillHive.Features.Categories.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // ─── Public Reads ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] string? academySlug = null)
        {
            try
            {
                var result = await _categoryService.GetCategoriesAsync(academySlug);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        [HttpGet("professions")]
        public async Task<IActionResult> GetProfessions(
            [FromQuery] string? academySlug = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                var result = await _categoryService.GetProfessionsAsync(academySlug, categoryId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Category Writes ───────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.CreateCategoryAsync(userId, role, academyId, dto);
                return ApiResponse.Created(result, "Category created");
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

        [HttpPatch("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.UpdateCategoryAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Category updated");
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> DeactivateCategory(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.DeactivateCategoryAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Category deactivated");
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

        // ─── Profession Writes ─────────────────────────────────────
        [HttpPost("professions")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> CreateProfession([FromBody] CreateProfessionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.CreateProfessionAsync(userId, role, academyId, dto);
                return ApiResponse.Created(result, "Profession created");
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

        [HttpPatch("professions/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> UpdateProfession(int id, [FromBody] UpdateProfessionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.UpdateProfessionAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Profession updated");
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

        [HttpDelete("professions/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> DeactivateProfession(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _categoryService.DeactivateProfessionAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Profession deactivated");
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