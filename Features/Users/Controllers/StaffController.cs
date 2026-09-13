using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Users.DTOs;
using SkillHive.Features.Users.Services;

namespace SkillHive.Features.Users.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly StaffService _staffService;

        public StaffController(StaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpPost("invite")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Invite([FromBody] InviteStaffDto dto)
        {
            try
            {
                var ownerId = JwtHelper.GetUserId(User);
                var result = await _staffService.InviteStaffAsync(ownerId, dto);
                return ApiResponse.Created(result, "Invitation sent");
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

        [HttpGet]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var staff = await _staffService.GetStaffListAsync(userId);
                return ApiResponse.Success(staff);
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
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffDto dto)
        {
            try
            {
                var ownerId = JwtHelper.GetUserId(User);
                var result = await _staffService.UpdateStaffAsync(ownerId, id, dto);
                return ApiResponse.Success(result, "Staff updated");
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

        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var ownerId = JwtHelper.GetUserId(User);
                var result = await _staffService.DeactivateStaffAsync(ownerId, id);
                return ApiResponse.Success(result, "Staff deactivated");
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

        [HttpPatch("{id}/reactivate")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                var ownerId = JwtHelper.GetUserId(User);
                var result = await _staffService.ReactivateStaffAsync(ownerId, id);
                return ApiResponse.Success(result, "Staff reactivated");
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
    }
}