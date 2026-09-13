using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Academies.DTOs;
using SkillHive.Features.Academies.Services;

namespace SkillHive.Features.Academies.Controllers
{
    [ApiController]
    [Route("api/academies")]
    public class AcademiesController : ControllerBase
    {
        private readonly AcademyService _academyService;

        public AcademiesController(AcademyService academyService)
        {
            _academyService = academyService;
        }

        // ─── Public ───────────────────────────────────────────────
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetPublicProfile(string slug)
        {
            try
            {
                var result = await _academyService.GetPublicProfileAsync(slug);
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

        // ─── Private (owner only) ─────────────────────────────────
        [HttpGet("me")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var academy = await _academyService.GetOwnAcademyAsync(userId);
                return ApiResponse.Success(academy);
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

        [HttpPatch("me")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateAcademyDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var academy = await _academyService.UpdateOwnAcademyAsync(userId, dto);
                return ApiResponse.Success(academy, "Academy updated");
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