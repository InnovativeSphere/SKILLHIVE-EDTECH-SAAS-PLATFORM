using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Subscriptions.DTOs;
using SkillHive.Features.Subscriptions.Services;

namespace SkillHive.Features.Subscriptions.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionsController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // ─── PUBLIC — PLAN CATALOG ─────────────────────────

        /// <summary>
        /// Public pricing page. Returns active + public plans only.
        /// No auth required — this is the marketing surface.
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPlans()
        {
            try
            {
                var result = await _subscriptionService.GetPublicPlansAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Single plan by URL slug (e.g. /pricing/pro).
        /// No auth required.
        /// </summary>
        [HttpGet("plans/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlanBySlug(string slug)
        {
            try
            {
                var result = await _subscriptionService.GetPlanBySlugAsync(slug);
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

        // ─── SUPERADMIN — PLAN MANAGEMENT ──────────────────
        // NOTE: "all" and "id/{id}" declared before "plans/{slug}" patterns
        // so ASP.NET Core's matcher picks the most specific route first.

        /// <summary>
        /// Superadmin — full plan listing including inactive/hidden plans.
        /// </summary>
        [HttpGet("plans/all")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetAllPlans()
        {
            try
            {
                var result = await _subscriptionService.GetAllPlansAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Superadmin — fetch a plan by numeric ID (used after listing for edit).
        /// </summary>
        [HttpGet("plans/id/{id:int}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            try
            {
                var result = await _subscriptionService.GetPlanAsync(id);
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

        /// <summary>
        /// Superadmin — create a new plan.
        /// </summary>
        [HttpPost("plans")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto)
        {
            try
            {
                var result = await _subscriptionService.CreatePlanAsync(dto);
                return ApiResponse.Created(result, "Plan created");
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

        /// <summary>
        /// Superadmin — partial update. Slug is intentionally immutable.
        /// Price change blocked if active subscriptions exist.
        /// </summary>
        [HttpPatch("plans/{id:int}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanDto dto)
        {
            try
            {
                var result = await _subscriptionService.UpdatePlanAsync(id, dto);
                return ApiResponse.Success(result, "Plan updated");
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

        /// <summary>
        /// Superadmin — soft delete (IsActive = false).
        /// Blocked if any active subscriptions exist.
        /// </summary>
        [HttpDelete("plans/{id:int}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> DeactivatePlan(int id)
        {
            try
            {
                var result = await _subscriptionService.DeactivatePlanAsync(id);
                return ApiResponse.Success(result, "Plan deactivated");
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

        // ─── SUPERADMIN — SUBSCRIPTION ADMINISTRATION ──────

        /// <summary>
        /// Superadmin — assign a plan to an academy (creates or updates the existing row).
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> AssignSubscription([FromBody] AssignSubscriptionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _subscriptionService.AssignSubscriptionAsync(dto, userId);
                return ApiResponse.Created(result, "Subscription assigned");
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

        /// <summary>
        /// Superadmin — move a subscription to a different plan.
        /// Upgrades apply immediately; downgrades blocked if current usage exceeds new plan.
        /// </summary>
        [HttpPost("{id:int}/change-plan")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> ChangePlan(int id, [FromBody] ChangePlanDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _subscriptionService.ChangePlanAsync(id, dto, userId);
                return ApiResponse.Success(result, "Plan changed");
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

        // ─── OWNER — MY SUBSCRIPTION ───────────────────────

        /// <summary>
        /// Academy owner — read your own subscription.
        /// academyId is read from the JWT — no need to know it.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> GetMySubscription()
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _subscriptionService.GetMySubscriptionAsync(academyId.Value);
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

        /// <summary>
        /// Owner or superadmin — get a subscription by academy ID.
        /// Service enforces: owner can only fetch their own academy.
        /// </summary>
        [HttpGet("academy/{academyId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> GetSubscriptionForAcademy(int academyId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _subscriptionService.GetSubscriptionForAcademyAsync(
                    userId, role, requesterAcademyId, academyId);
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

        // ─── OWNER — LIFECYCLE ACTIONS ─────────────────────

        /// <summary>
        /// Owner or superadmin — cancel subscription at end of current period.
        /// Access continues until EndDate, then expires.
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> CancelSubscription(int id, [FromBody] CancelSubscriptionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _subscriptionService.CancelSubscriptionAsync(
                    id, userId, role, requesterAcademyId, dto.Reason);
                return ApiResponse.Success(result, "Subscription cancelled");
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

        /// <summary>
        /// Owner or superadmin — suspend subscription immediately.
        /// Reversible via /reactivate if EndDate hasn't passed.
        /// </summary>
        [HttpPost("{id:int}/suspend")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> SuspendSubscription(int id, [FromBody] SuspendSubscriptionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _subscriptionService.SuspendSubscriptionAsync(
                    id, userId, role, requesterAcademyId, dto.Reason);
                return ApiResponse.Success(result, "Subscription suspended");
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

        /// <summary>
        /// Owner or superadmin — reactivate a suspended subscription.
        /// Fails if EndDate has passed (must renew via payment instead).
        /// </summary>
        [HttpPost("{id:int}/reactivate")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> ReactivateSubscription(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _subscriptionService.ReactivateSubscriptionAsync(
                    id, userId, role, requesterAcademyId);
                return ApiResponse.Success(result, "Subscription reactivated");
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