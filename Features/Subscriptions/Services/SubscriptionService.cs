using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Subscriptions.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Subscriptions.Services
{
    public class SubscriptionService
    {
        private readonly AppDbContext _db;

        public SubscriptionService(AppDbContext db)
        {
            _db = db;
        }

        // ─── PLAN OPERATIONS ────────────────────────────────

        /// <summary>
        /// Create a new plan. Superadmin only (enforced at controller).
        /// Slug auto-generated from Name if not provided, then ensured unique.
        /// </summary>
        public async Task<object> CreatePlanAsync(CreatePlanDto dto)
        {
            var name = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));
            var baseSlug = !string.IsNullOrWhiteSpace(dto.Slug)
                ? SlugHelper.GenerateSlug(dto.Slug)
                : SlugHelper.GenerateSlug(name);

            var slug = await SlugHelper.EnsureUniqueSlugAsync(
                baseSlug,
                async s => await _db.Plans.AnyAsync(p => p.Slug == s));

            var plan = new Plan
            {
                Name = name,
                Slug = slug,
                Description = dto.Description != null ? Utils.SanitizeInput(dto.Description) : null,
                Price = dto.Price,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "NGN" : dto.Currency.ToUpperInvariant(),
                Interval = dto.Interval,
                MaxCourses = dto.MaxCourses,
                MaxStaff = dto.MaxStaff,
                MaxStudentsPerCourse = dto.MaxStudentsPerCourse,
                CanChargeCourses = dto.CanChargeCourses,
                CanUseCertificates = dto.CanUseCertificates,
                CanUseCustomBranding = dto.CanUseCustomBranding,
                IsActive = dto.IsActive,
                IsPublic = dto.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Plans.Add(plan);
            await _db.SaveChangesAsync();

            return PlanToResponse(plan);
        }

        /// <summary>
        /// Public listing — only active + public plans, sorted by price.
        /// </summary>
        public async Task<List<object>> GetPublicPlansAsync()
        {
            var plans = await _db.Plans
                .Where(p => p.IsActive && p.IsPublic)
                .OrderBy(p => p.Price)
                .ToListAsync();

            return plans.Select(p => PlanToResponse(p)).Cast<object>().ToList();
        }

        /// <summary>
        /// Superadmin listing — includes inactive/hidden plans.
        /// </summary>
        public async Task<List<object>> GetAllPlansAsync()
        {
            var plans = await _db.Plans
                .OrderBy(p => p.Price)
                .ToListAsync();

            return plans.Select(p => PlanToResponse(p)).Cast<object>().ToList();
        }

        /// <summary>
        /// Single plan by ID. Public for active ones; superadmin can see any.
        /// </summary>
        public async Task<object> GetPlanAsync(int planId)
        {
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            return PlanToResponse(plan);
        }

        public async Task<object> GetPlanBySlugAsync(string slug)
        {
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Slug == slug);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            return PlanToResponse(plan);
        }

        /// <summary>
        /// Update plan. Slug is intentionally not editable — see DTO comment.
        /// Price change allowed but flagged if active subscriptions exist.
        /// </summary>
        public async Task<object> UpdatePlanAsync(int planId, UpdatePlanDto dto)
        {
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                plan.Name = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));

            if (dto.Description != null)
                plan.Description = Utils.SanitizeInput(dto.Description);

            if (dto.Price.HasValue)
            {
                var hasActiveSubs = await _db.Subscriptions
                    .AnyAsync(s => s.PlanId == planId
                                   && (s.Status == SubscriptionStatus.ACTIVE
                                       || s.Status == SubscriptionStatus.TRIAL));

                if (hasActiveSubs)
                    throw new InvalidOperationException(
                        "Cannot change price while active subscriptions exist. Create a new plan instead.");

                plan.Price = dto.Price.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Currency))
                plan.Currency = dto.Currency.ToUpperInvariant();

            if (dto.Interval.HasValue) plan.Interval = dto.Interval.Value;
            if (dto.MaxCourses.HasValue) plan.MaxCourses = dto.MaxCourses.Value;
            if (dto.MaxStaff.HasValue) plan.MaxStaff = dto.MaxStaff.Value;
            if (dto.MaxStudentsPerCourse.HasValue) plan.MaxStudentsPerCourse = dto.MaxStudentsPerCourse.Value;
            if (dto.CanChargeCourses.HasValue) plan.CanChargeCourses = dto.CanChargeCourses.Value;
            if (dto.CanUseCertificates.HasValue) plan.CanUseCertificates = dto.CanUseCertificates.Value;
            if (dto.CanUseCustomBranding.HasValue) plan.CanUseCustomBranding = dto.CanUseCustomBranding.Value;
            if (dto.IsActive.HasValue) plan.IsActive = dto.IsActive.Value;
            if (dto.IsPublic.HasValue) plan.IsPublic = dto.IsPublic.Value;

            plan.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return PlanToResponse(plan);
        }

        /// <summary>
        /// Soft-delete: sets IsActive = false. Blocked if any active subscriptions use it.
        /// </summary>
        public async Task<object> DeactivatePlanAsync(int planId)
        {
            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            if (!plan.IsActive)
                throw new InvalidOperationException("Plan is already deactivated");

            var activeSubCount = await _db.Subscriptions
                .CountAsync(s => s.PlanId == planId
                                 && (s.Status == SubscriptionStatus.ACTIVE
                                     || s.Status == SubscriptionStatus.TRIAL
                                     || s.Status == SubscriptionStatus.GRACE
                                     || s.Status == SubscriptionStatus.PAST_DUE));

            if (activeSubCount > 0)
                throw new InvalidOperationException(
                    $"Cannot deactivate plan with {activeSubCount} active subscription(s). Migrate them first.");

            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { planId = plan.PlanId, isActive = false, message = "Plan deactivated" };
        }

        // ─── SUBSCRIPTION OPERATIONS ────────────────────────

        /// <summary>
        /// Superadmin manual assignment. If the academy already has a subscription,
        /// this updates it in place (one per academy rule).
        /// </summary>
        public async Task<object> AssignSubscriptionAsync(AssignSubscriptionDto dto)
        {
            var academy = await _db.Academies.FirstOrDefaultAsync(a => a.AcademyId == dto.AcademyId);
            if (academy == null)
                throw new InvalidOperationException("Academy not found");

            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == dto.PlanId);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            if (!plan.IsActive)
                throw new InvalidOperationException("Cannot assign an inactive plan");

            var endDate = dto.EndDate ?? DateHelper.CalculateSubscriptionEndDate(dto.StartDate, plan.Interval);

            if (endDate <= dto.StartDate)
                throw new InvalidOperationException("End date must be after start date");

            var existing = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.AcademyId == dto.AcademyId);

            if (existing != null)
            {
                existing.PlanId = dto.PlanId;
                existing.StartDate = dto.StartDate;
                existing.EndDate = endDate;
                existing.Status = SubscriptionStatus.ACTIVE;
                existing.AutoRenew = dto.AutoRenew;
                existing.GraceUntil = dto.GraceUntil;
                existing.CancelledAt = null;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return SubscriptionToResponse(existing, plan);
            }

            var subscription = new Subscription
            {
                AcademyId = dto.AcademyId,
                PlanId = dto.PlanId,
                StartDate = dto.StartDate,
                EndDate = endDate,
                Status = SubscriptionStatus.ACTIVE,
                AutoRenew = dto.AutoRenew,
                GraceUntil = dto.GraceUntil,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            return SubscriptionToResponse(subscription, plan);
        }

        /// <summary>
        /// Owner gets their own subscription. Superadmin can specify any academyId.
        /// </summary>
        public async Task<object> GetSubscriptionForAcademyAsync(
            int requesterUserId, UserRole role, int? requesterAcademyId, int targetAcademyId)
        {
            // Non-superadmin can only fetch their own
            if (role != UserRole.SUPER_ADMIN && requesterAcademyId != targetAcademyId)
                throw new UnauthorizedAccessException("You can only view your own academy's subscription");

            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Academy)
                .FirstOrDefaultAsync(s => s.AcademyId == targetAcademyId);

            if (subscription == null)
                throw new InvalidOperationException("No subscription found for this academy");

            return SubscriptionToResponse(subscription, subscription.Plan, subscription.Academy);
        }

                /// <summary>
        /// Owner shortcut — reads academyId from the JWT context.
        /// Prevents the owner from needing to know their own academyId.
        /// </summary>
        public async Task<object> GetMySubscriptionAsync(int requesterAcademyId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Academy)
                .FirstOrDefaultAsync(s => s.AcademyId == requesterAcademyId);

            if (subscription == null)
                throw new InvalidOperationException("No subscription found for your academy");

            return SubscriptionToResponse(subscription, subscription.Plan, subscription.Academy);
        }

        /// <summary>
        /// Change plan mid-cycle. Upgrades apply immediately; downgrades blocked if
        /// current usage exceeds new plan's limits.
        /// </summary>
        public async Task<object> ChangePlanAsync(int subscriptionId, ChangePlanDto dto)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (subscription.Status == SubscriptionStatus.CANCELLED
                || subscription.Status == SubscriptionStatus.SUSPENDED)
                throw new InvalidOperationException(
                    $"Cannot change plan while subscription is {subscription.Status}");

            var newPlan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == dto.NewPlanId);
            if (newPlan == null)
                throw new InvalidOperationException("New plan not found");

            if (!newPlan.IsActive)
                throw new InvalidOperationException("Cannot switch to an inactive plan");

            if (newPlan.PlanId == subscription.PlanId)
                throw new InvalidOperationException("Academy is already on this plan");

            // Block downgrade if current usage exceeds new plan's limits
            var isDowngrade = newPlan.Price < subscription.Plan.Price;
            if (isDowngrade)
            {
                var courseCount = await _db.Courses
                    .CountAsync(c => c.AcademyId == subscription.AcademyId
                                     && c.Status != CourseStatus.ARCHIVED);
                var staffCount = await _db.Users
                    .CountAsync(u => u.AcademyId == subscription.AcademyId
                                     && u.Status != UserStatus.INACTIVE);

                if (newPlan.MaxCourses.HasValue && courseCount > newPlan.MaxCourses.Value)
                    throw new InvalidOperationException(
                        $"Cannot downgrade: academy has {courseCount} courses, new plan allows {newPlan.MaxCourses.Value}");

                if (newPlan.MaxStaff.HasValue && staffCount > newPlan.MaxStaff.Value)
                    throw new InvalidOperationException(
                        $"Cannot downgrade: academy has {staffCount} staff, new plan allows {newPlan.MaxStaff.Value}");
            }

            subscription.PlanId = dto.NewPlanId;

            // Upgrades: reset end date from now, based on new plan's interval
            if (!isDowngrade)
            {
                subscription.StartDate = DateTime.UtcNow;
                subscription.EndDate = DateHelper.CalculateSubscriptionEndDate(DateTime.UtcNow, newPlan.Interval);
                subscription.Status = SubscriptionStatus.ACTIVE;
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return SubscriptionToResponse(subscription, newPlan);
        }

        /// <summary>
        /// Cancel subscription. Keeps it in the DB, sets status to CANCELLED.
        /// Owner can cancel their own; superadmin can cancel any.
        /// </summary>
        public async Task<object> CancelSubscriptionAsync(
            int subscriptionId, int requesterUserId, UserRole role, int? requesterAcademyId, string reason)
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (role != UserRole.SUPER_ADMIN && subscription.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You can only cancel your own academy's subscription");

            if (subscription.Status == SubscriptionStatus.CANCELLED)
                throw new InvalidOperationException("Subscription is already cancelled");

            subscription.Status = SubscriptionStatus.CANCELLED;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.AutoRenew = false;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                subscriptionId = subscription.SubscriptionId,
                status = subscription.Status.ToString(),
                cancelledAt = subscription.CancelledAt,
                reason = Utils.SanitizeInput(reason),
                message = "Subscription cancelled"
            };
        }

        /// <summary>
        /// Superadmin-only. Used for policy violations or admin actions.
        /// </summary>
             /// <summary>
        /// Suspend subscription. Owner can suspend their own; superadmin can suspend any.
        /// Immediate effect — no access until reactivated.
        /// </summary>
        public async Task<object> SuspendSubscriptionAsync(
            int subscriptionId, int requesterUserId, UserRole role, int? requesterAcademyId, string reason)
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (role != UserRole.SUPER_ADMIN && subscription.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You can only suspend your own academy's subscription");

            if (subscription.Status == SubscriptionStatus.SUSPENDED)
                throw new InvalidOperationException("Subscription is already suspended");

            if (subscription.Status == SubscriptionStatus.CANCELLED)
                throw new InvalidOperationException("Cannot suspend a cancelled subscription");

            subscription.Status = SubscriptionStatus.SUSPENDED;
            subscription.AutoRenew = false;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                subscriptionId = subscription.SubscriptionId,
                status = subscription.Status.ToString(),
                reason = Utils.SanitizeInput(reason),
                message = "Subscription suspended. You can reactivate it any time before the end date."
            };
        }

                /// <summary>
        /// Reactivate a suspended subscription. Owner can reactivate their own;
        /// superadmin can reactivate any.
        /// Fails if the end date has passed — in that case, use Payments to renew.
        /// </summary>
        public async Task<object> ReactivateSubscriptionAsync(
            int subscriptionId, int requesterUserId, UserRole role, int? requesterAcademyId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (role != UserRole.SUPER_ADMIN && subscription.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You can only reactivate your own academy's subscription");

            if (subscription.Status != SubscriptionStatus.SUSPENDED)
                throw new InvalidOperationException(
                    $"Cannot reactivate a subscription with status {subscription.Status}");

            if (subscription.EndDate <= DateTime.UtcNow)
                throw new InvalidOperationException(
                    "Subscription has expired. Please renew to restore access.");

            subscription.Status = SubscriptionStatus.ACTIVE;
            subscription.AutoRenew = true;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                subscriptionId = subscription.SubscriptionId,
                status = subscription.Status.ToString(),
                endDate = subscription.EndDate,
                daysRemaining = DateHelper.DaysUntil(subscription.EndDate),
                message = "Subscription reactivated"
            };
        }

        /// <summary>
        /// Called by Payments on successful invoice payment.
        /// Moves subscription to ACTIVE and extends end date by plan interval.
        /// </summary>
        public async Task<object> ActivateSubscriptionAsync(int subscriptionId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            var now = DateTime.UtcNow;
            subscription.Status = SubscriptionStatus.ACTIVE;
            subscription.StartDate = now;
            subscription.EndDate = DateHelper.CalculateSubscriptionEndDate(now, subscription.Plan.Interval);
            subscription.GraceUntil = null;
            subscription.CancelledAt = null;
            subscription.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return SubscriptionToResponse(subscription, subscription.Plan);
        }

        // ─── TRIAL CREATION ────────────────────────────────

        /// <summary>
        /// Called by AuthService when an academy registers, and by SeedDemoAcademy.
        /// Creates a 7-day trial on the given plan. Idempotent — no-op if a subscription exists.
        /// </summary>
        public async Task<object> CreateTrialForAcademyAsync(int academyId, int? planId = null)
        {
            var existing = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.AcademyId == academyId);

            if (existing != null)
                return SubscriptionToResponse(existing);

            // Default to Starter if no planId provided
            int resolvedPlanId;
            if (planId.HasValue)
            {
                resolvedPlanId = planId.Value;
            }
            else
            {
                var starter = await _db.Plans.FirstOrDefaultAsync(p => p.Slug == "starter" && p.IsActive);
                if (starter == null)
                    throw new InvalidOperationException("Default Starter plan not found. Seed plans first.");
                resolvedPlanId = starter.PlanId;
            }

            var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanId == resolvedPlanId);
            if (plan == null)
                throw new InvalidOperationException("Plan not found");

            var now = DateTime.UtcNow;
            var subscription = new Subscription
            {
                AcademyId = academyId,
                PlanId = resolvedPlanId,
                StartDate = now,
                EndDate = DateHelper.CalculateTrialEndDate(now, 7),
                Status = SubscriptionStatus.TRIAL,
                AutoRenew = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            return SubscriptionToResponse(subscription, plan);
        }

        // ─── ENFORCEMENT HELPERS ────────────────────────────

        /// <summary>
        /// Used by Courses module before allowing a new course.
        /// Returns (allowed, reason). Reason is null if allowed.
        /// </summary>
        public async Task<(bool allowed, string? reason)> CanAddCourseAsync(int academyId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.AcademyId == academyId);

            if (subscription == null)
                return (false, "No active subscription");

            if (subscription.Status == SubscriptionStatus.EXPIRED
                || subscription.Status == SubscriptionStatus.CANCELLED
                || subscription.Status == SubscriptionStatus.SUSPENDED)
                return (false, $"Subscription is {subscription.Status}");

            if (subscription.Plan.MaxCourses.HasValue)
            {
                var count = await _db.Courses
                    .CountAsync(c => c.AcademyId == academyId
                                     && c.Status != CourseStatus.ARCHIVED);

                if (count >= subscription.Plan.MaxCourses.Value)
                    return (false, $"Plan limit reached: {subscription.Plan.MaxCourses.Value} courses maximum");
            }

            return (true, null);
        }

        /// <summary>
        /// Used by Users/Staff module before inviting new staff.
        /// </summary>
        public async Task<(bool allowed, string? reason)> CanAddStaffAsync(int academyId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.AcademyId == academyId);

            if (subscription == null)
                return (false, "No active subscription");

            if (subscription.Status == SubscriptionStatus.EXPIRED
                || subscription.Status == SubscriptionStatus.CANCELLED
                || subscription.Status == SubscriptionStatus.SUSPENDED)
                return (false, $"Subscription is {subscription.Status}");

            if (subscription.Plan.MaxStaff.HasValue)
            {
                var count = await _db.Users
                    .CountAsync(u => u.AcademyId == academyId
                                     && u.Status != UserStatus.INACTIVE);

                if (count >= subscription.Plan.MaxStaff.Value)
                    return (false, $"Plan limit reached: {subscription.Plan.MaxStaff.Value} staff maximum");
            }

            return (true, null);
        }

        /// <summary>
        /// Used by Enrollments module before allowing a new enrollment.
        /// Only counts ACTIVE + COMPLETED enrollments against the cap.
        /// </summary>
        public async Task<(bool allowed, string? reason)> CanEnrollStudentAsync(int academyId, int courseId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.AcademyId == academyId);

            if (subscription == null)
                return (false, "No active subscription");

            if (subscription.Status == SubscriptionStatus.EXPIRED
                || subscription.Status == SubscriptionStatus.CANCELLED
                || subscription.Status == SubscriptionStatus.SUSPENDED)
                return (false, $"Subscription is {subscription.Status}");

            if (subscription.Plan.MaxStudentsPerCourse.HasValue)
            {
                var count = await _db.Enrollments
                    .CountAsync(e => e.CourseId == courseId
                                     && (e.Status == EnrollmentStatus.ACTIVE
                                         || e.Status == EnrollmentStatus.COMPLETED));

                if (count >= subscription.Plan.MaxStudentsPerCourse.Value)
                    return (false, $"Plan limit reached: {subscription.Plan.MaxStudentsPerCourse.Value} students per course maximum");
            }

            return (true, null);
        }

        /// <summary>
        /// Quick check — is the academy's subscription in a usable state?
        /// Used by various modules as a preliminary guard.
        /// </summary>
        public async Task<bool> IsSubscriptionUsableAsync(int academyId)
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.AcademyId == academyId);

            if (subscription == null) return false;

            return subscription.Status == SubscriptionStatus.TRIAL
                || subscription.Status == SubscriptionStatus.ACTIVE
                || subscription.Status == SubscriptionStatus.GRACE
                || subscription.Status == SubscriptionStatus.PAST_DUE;
        }

        // ─── HELPERS ─────────────────────────────────────────

        private static object PlanToResponse(Plan p)
        {
            return new
            {
                planId = p.PlanId,
                name = p.Name,
                slug = p.Slug,
                description = p.Description,
                price = p.Price,
                currency = p.Currency,
                interval = p.Interval.ToString(),
                maxCourses = p.MaxCourses,
                maxStaff = p.MaxStaff,
                maxStudentsPerCourse = p.MaxStudentsPerCourse,
                canChargeCourses = p.CanChargeCourses,
                canUseCertificates = p.CanUseCertificates,
                canUseCustomBranding = p.CanUseCustomBranding,
                isActive = p.IsActive,
                isPublic = p.IsPublic,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt
            };
        }

        private static object SubscriptionToResponse(
            Subscription s, Plan? plan = null, Academy? academy = null)
        {
            var daysRemaining = DateHelper.DaysUntil(s.EndDate);

            return new
            {
                subscriptionId = s.SubscriptionId,
                academyId = s.AcademyId,
                academyName = academy?.Name,
                planId = s.PlanId,
                planName = plan?.Name,
                planSlug = plan?.Slug,
                status = s.Status.ToString(),
                startDate = s.StartDate,
                endDate = s.EndDate,
                daysRemaining,
                autoRenew = s.AutoRenew,
                graceUntil = s.GraceUntil,
                cancelledAt = s.CancelledAt,
                createdAt = s.CreatedAt,
                updatedAt = s.UpdatedAt
            };
        }
                /// <summary>
        /// Called by Payments/Invoices on successful payment for a renewal.
        /// Extends the subscription from the current EndDate (or now, whichever is later).
        /// Use this for renewals; use ActivateSubscriptionAsync for trial → active or
        /// reactivation from expiry.
        /// </summary>
        public async Task<object> ExtendSubscriptionAsync(int subscriptionId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            var now = DateTime.UtcNow;
            // Extend from current EndDate if still in the future; otherwise from now
            var baseDate = subscription.EndDate > now ? subscription.EndDate : now;

            subscription.Status = SubscriptionStatus.ACTIVE;
            subscription.EndDate = DateHelper.CalculateSubscriptionEndDate(baseDate, subscription.Plan.Interval);
            subscription.GraceUntil = null;
            subscription.CancelledAt = null;
            subscription.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return SubscriptionToResponse(subscription, subscription.Plan);
        }
    }
}