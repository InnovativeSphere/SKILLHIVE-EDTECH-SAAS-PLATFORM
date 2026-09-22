using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Reviews.DTOs;
using SkillHive.Features.Reviews.Services;

namespace SkillHive.Features.Reviews.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly ReviewService _reviewService;

        public ReviewsController(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ─── Create Review (Student) ────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _reviewService.CreateReviewAsync(userId, dto);
                return ApiResponse.Created(result, "Review submitted");
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

        // ─── My Reviews (Student) ──────────────────────────────────
        [HttpGet("my")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _reviewService.GetMyReviewsAsync(userId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Academy Reviews (Owner/Instructor/Moderator) ──────────
        [HttpGet("academy")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> GetAcademyReviews(
            [FromQuery] int? courseId,
            [FromQuery] string? status)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null) return ApiResponse.Forbidden("You do not belong to an academy");

                ReviewStatus? parsedStatus = null;
                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<ReviewStatus>(status.ToUpperInvariant(), out var s))
                {
                    parsedStatus = s;
                }

                var result = await _reviewService.GetAcademyReviewsAsync(academyId.Value, courseId, parsedStatus);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Course Reviews (Public, paginated) ────────────────────
        [HttpGet("course/{courseId:int}")]
        public async Task<IActionResult> GetCourseReviews(
            int courseId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var result = await _reviewService.GetCourseReviewsAsync(courseId, page, pageSize);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Get One Review ────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReview(int id)
        {
            try
            {
                var result = await _reviewService.GetReviewAsync(id);
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

        // ─── Update Own Review (Student) ────────────────────────────
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _reviewService.UpdateReviewAsync(userId, id, dto);
                return ApiResponse.Success(result, "Review updated");
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

        // ─── Hide Review (Owner/Moderator/Superadmin) ──────────────
        [HttpPost("{id:int}/hide")]
        [Authorize(Roles = "ACADEMY_OWNER,MODERATOR,SUPER_ADMIN")]
        public async Task<IActionResult> Hide(int id, [FromBody] HideReviewDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _reviewService.HideReviewAsync(id, userId, role, academyId, dto.Reason);
                return ApiResponse.Success(result, "Review hidden");
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

        // ─── Unhide Review (Owner/Moderator/Superadmin) ────────────
        [HttpPost("{id:int}/unhide")]
        [Authorize(Roles = "ACADEMY_OWNER,MODERATOR,SUPER_ADMIN")]
        public async Task<IActionResult> Unhide(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _reviewService.UnhideReviewAsync(id, userId, role, academyId);
                return ApiResponse.Success(result, "Review restored");
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