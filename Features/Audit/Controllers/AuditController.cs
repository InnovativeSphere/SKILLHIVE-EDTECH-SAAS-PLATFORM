using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Audit.Services;

namespace SkillHive.Features.Audit.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly AuditService _auditService;

        public AuditController(AuditService auditService)
        {
            _auditService = auditService;
        }

        // ─── SUPERADMIN — PLATFORM-WIDE ─────────────────────

        /// <summary>
        /// Superadmin — full audit log with filters.
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? userId,
            [FromQuery] int? academyId,
            [FromQuery] string? action,
            [FromQuery] string? targetType,
            [FromQuery] int? targetId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var result = await _auditService.GetAllAsync(
                    userId, academyId, action, targetType, targetId,
                    dateFrom, dateTo, page, pageSize);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── OWNER — ACADEMY-SCOPED ─────────────────────────

        /// <summary>
        /// Academy owner — audit log scoped to your own academy.
        /// </summary>
        [HttpGet("academy")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> GetAcademyLogs(
            [FromQuery] int? userId,
            [FromQuery] string? action,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _auditService.GetForAcademyAsync(
                    academyId.Value, userId, action, page, pageSize);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── ANY AUTHENTICATED USER — OWN HISTORY ───────────

        /// <summary>
        /// Current user — your own recent actions.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyLogs(
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _auditService.GetForUserAsync(userId, page, pageSize);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── SUPERADMIN or OWNER — SINGLE ENTRY ─────────────

        /// <summary>
        /// Single audit log entry. Owner can only see their academy's entries.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> GetOne(int id)
        {
            try
            {
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _auditService.GetOneAsync(id, role, academyId);
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
    }
}