using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Audit.Services;

namespace SkillHive.Features.Audit.Controllers
{
    [ApiController]
    [Route("api/system")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public class SystemAdminController : ControllerBase
    {
        private readonly SystemAdminService _systemAdminService;

        public SystemAdminController(SystemAdminService systemAdminService)
        {
            _systemAdminService = systemAdminService;
        }

        /// <summary>
        /// Superadmin health check — DB connectivity, uptime.
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var result = await _systemAdminService.GetHealthAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Superadmin — platform version and environment info.
        /// </summary>
        [HttpGet("info")]
        public IActionResult Info()
        {
            try
            {
                var result = _systemAdminService.GetSystemInfo();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }
    }
}