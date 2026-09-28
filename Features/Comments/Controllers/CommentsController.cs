using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Comments.DTOs;
using SkillHive.Features.Comments.Services;

namespace SkillHive.Features.Comments.Controllers
{
    [ApiController]
    [Route("api/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly CommentService _commentService;

        public CommentsController(CommentService commentService)
        {
            _commentService = commentService;
        }

        // ─── Create Comment (Enrolled Students + Academy Staff) ────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateCommentDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _commentService.CreateCommentAsync(userId, role, academyId, dto);
                return ApiResponse.Created(result, "Comment posted");
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

        // ─── List Comments for a Target (Public) ───────────────────
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string targetType,
            [FromQuery] int targetId)
        {
            try
            {
                if (!Enum.TryParse<CommentTargetType>(targetType.ToUpperInvariant(), out var parsedType))
                    return ApiResponse.BadRequest("targetType must be COURSE or LESSON");

                var result = await _commentService.ListCommentsAsync(parsedType, targetId);
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

        // ─── Get One Comment ───────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var result = await _commentService.GetCommentAsync(id);
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

        // ─── Update Own Comment ────────────────────────────────────
        [HttpPatch("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _commentService.UpdateCommentAsync(userId, id, dto);
                return ApiResponse.Success(result, "Comment updated");
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

        // ─── Delete Own Comment (soft) ─────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _commentService.DeleteOwnCommentAsync(userId, id);
                return ApiResponse.Success(result, "Comment deleted");
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

        // ─── Hide Comment (Owner/Moderator/Superadmin) ─────────────
        [HttpPost("{id:int}/hide")]
        [Authorize(Roles = "ACADEMY_OWNER,MODERATOR,SUPER_ADMIN")]
        public async Task<IActionResult> Hide(int id, [FromBody] HideCommentDto dto)
        {
            try
            {
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _commentService.HideCommentAsync(id, role, academyId, dto.Reason);
                return ApiResponse.Success(result, "Comment hidden");
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

        // ─── Unhide Comment ────────────────────────────────────────
        [HttpPost("{id:int}/unhide")]
        [Authorize(Roles = "ACADEMY_OWNER,MODERATOR,SUPER_ADMIN")]
        public async Task<IActionResult> Unhide(int id)
        {
            try
            {
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _commentService.UnhideCommentAsync(id, role, academyId);
                return ApiResponse.Success(result, "Comment restored");
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

        // ─── Pin/Unpin Comment (Owner/Moderator/Superadmin) ────────
        [HttpPost("{id:int}/pin")]
        [Authorize(Roles = "ACADEMY_OWNER,MODERATOR,SUPER_ADMIN")]
        public async Task<IActionResult> Pin(int id, [FromBody] PinCommentDto dto)
        {
            try
            {
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _commentService.PinCommentAsync(id, role, academyId, dto.IsPinned);
                return ApiResponse.Success(result, dto.IsPinned ? "Comment pinned" : "Comment unpinned");
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