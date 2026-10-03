using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Audit.Services;
using SkillHive.Features.Invoices.DTOs;
using SkillHive.Features.Subscriptions.Services;
using SkillHive.Models;

namespace SkillHive.Features.Invoices.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _db;
        private readonly SubscriptionService _subscriptions;
        private readonly AuditService _audit;

        public InvoiceService(AppDbContext db, SubscriptionService subscriptions, AuditService audit)
        {
            _db = db;
            _subscriptions = subscriptions;
            _audit = audit;
        }

        // ─── SUPERADMIN — MANUAL CREATION ───────────────────

        public async Task<object> CreateManualInvoiceAsync(CreateInvoiceDto dto, int superUserId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == dto.SubscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (dto.PeriodEnd <= dto.PeriodStart)
                throw new InvalidOperationException("Period end must be after period start");

            if (dto.DueDate < dto.PeriodStart)
                throw new InvalidOperationException("Due date cannot be before period start");

            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                SubscriptionId = subscription.SubscriptionId,
                AcademyId = subscription.AcademyId,
                PlanId = subscription.PlanId,
                AmountDue = dto.AmountDue,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "NGN" : dto.Currency.ToUpperInvariant(),
                Status = InvoiceStatus.UNPAID,
                IssuedAt = DateTime.UtcNow,
                DueDate = dto.DueDate,
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            // ─── AUDIT ───
            await _audit.LogAsync(
                userId: superUserId,
                academyId: invoice.AcademyId,
                action: AuditActions.InvoiceCreated,
                targetType: AuditTargetTypes.Invoice,
                targetId: invoice.InvoiceId,
                metadata: new
                {
                    invoiceNumber = invoice.InvoiceNumber,
                    amount = invoice.AmountDue,
                    source = "manual",
                    periodStart = invoice.PeriodStart,
                    periodEnd = invoice.PeriodEnd
                });

            return await InvoiceToResponseAsync(invoice.InvoiceId);
        }

        // ─── SUPERADMIN — AUTOMATED GENERATION ──────────────

        public async Task<object> GenerateNextInvoiceAsync(int subscriptionId, int superUserId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (subscription.Status == SubscriptionStatus.CANCELLED
                || subscription.Status == SubscriptionStatus.SUSPENDED)
                throw new InvalidOperationException(
                    $"Cannot generate an invoice for a {subscription.Status} subscription");

            var existingUnpaid = await _db.Invoices
                .AnyAsync(i => i.SubscriptionId == subscriptionId
                               && (i.Status == InvoiceStatus.UNPAID || i.Status == InvoiceStatus.DRAFT));

            if (existingUnpaid)
                throw new InvalidOperationException(
                    "Subscription already has an unpaid invoice. Settle it or void it first.");

            var now = DateTime.UtcNow;
            var periodStart = subscription.EndDate > now ? subscription.EndDate : now;
            var periodEnd = DateHelper.CalculateSubscriptionEndDate(periodStart, subscription.Plan.Interval);
            var dueDate = periodStart;

            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                SubscriptionId = subscription.SubscriptionId,
                AcademyId = subscription.AcademyId,
                PlanId = subscription.PlanId,
                AmountDue = subscription.Plan.Price,
                Currency = subscription.Plan.Currency,
                Status = InvoiceStatus.UNPAID,
                IssuedAt = now,
                DueDate = dueDate,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            // ─── AUDIT ───
            await _audit.LogAsync(
                userId: superUserId,
                academyId: invoice.AcademyId,
                action: AuditActions.InvoiceCreated,
                targetType: AuditTargetTypes.Invoice,
                targetId: invoice.InvoiceId,
                metadata: new
                {
                    invoiceNumber = invoice.InvoiceNumber,
                    amount = invoice.AmountDue,
                    source = "auto",
                    periodStart = invoice.PeriodStart,
                    periodEnd = invoice.PeriodEnd
                });

            return await InvoiceToResponseAsync(invoice.InvoiceId);
        }

        // ─── READS (unchanged) ──────────────────────────────

        public async Task<object> GetInvoiceAsync(
            int invoiceId, UserRole role, int? requesterAcademyId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (role != UserRole.SUPER_ADMIN && invoice.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You do not have permission to view this invoice");

            return await InvoiceToResponseAsync(invoiceId);
        }

        public async Task<List<object>> ListMyAcademyInvoicesAsync(int? requesterAcademyId)
        {
            if (requesterAcademyId == null)
                throw new InvalidOperationException("You do not belong to an academy");

            var invoices = await _db.Invoices
                .Where(i => i.AcademyId == requesterAcademyId.Value)
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => i.InvoiceId)
                .ToListAsync();

            return await BuildResponseListAsync(invoices);
        }

        public async Task<List<object>> ListInvoicesForSubscriptionAsync(
            int subscriptionId, UserRole role, int? requesterAcademyId)
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            if (role != UserRole.SUPER_ADMIN && subscription.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You do not have permission to view these invoices");

            var invoices = await _db.Invoices
                .Where(i => i.SubscriptionId == subscriptionId)
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => i.InvoiceId)
                .ToListAsync();

            return await BuildResponseListAsync(invoices);
        }

        public async Task<List<object>> ListAllInvoicesAsync(
            InvoiceStatus? status, int? academyId)
        {
            var query = _db.Invoices.AsQueryable();

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            if (academyId.HasValue)
                query = query.Where(i => i.AcademyId == academyId.Value);

            var invoiceIds = await query
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => i.InvoiceId)
                .ToListAsync();

            return await BuildResponseListAsync(invoiceIds);
        }

        // ─── SUPERADMIN — LIFECYCLE ACTIONS ─────────────────

        public async Task<object> VoidInvoiceAsync(int invoiceId, VoidInvoiceDto dto, int superUserId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (invoice.Status == InvoiceStatus.PAID)
                throw new InvalidOperationException("Cannot void a paid invoice. Issue a refund instead.");

            if (invoice.Status == InvoiceStatus.VOID)
                throw new InvalidOperationException("Invoice is already voided");

            invoice.Status = InvoiceStatus.VOID;
            invoice.VoidedAt = DateTime.UtcNow;
            invoice.VoidedReason = Utils.SanitizeInput(dto.Reason);
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // ─── AUDIT ───
            await _audit.LogAsync(
                userId: superUserId,
                academyId: invoice.AcademyId,
                action: AuditActions.InvoiceVoided,
                targetType: AuditTargetTypes.Invoice,
                targetId: invoice.InvoiceId,
                metadata: new
                {
                    invoiceNumber = invoice.InvoiceNumber,
                    reason = invoice.VoidedReason
                });

            return await InvoiceToResponseAsync(invoiceId);
        }

        public async Task<object> MarkPaidAsync(int invoiceId, MarkInvoicePaidDto dto, int superUserId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (invoice.Status == InvoiceStatus.PAID)
                throw new InvalidOperationException("Invoice is already paid");

            if (invoice.Status == InvoiceStatus.VOID)
                throw new InvalidOperationException("Cannot pay a voided invoice");

            invoice.Status = InvoiceStatus.PAID;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // ─── AUDIT ───
            await _audit.LogAsync(
                userId: superUserId,
                academyId: invoice.AcademyId,
                action: AuditActions.InvoicePaid,
                targetType: AuditTargetTypes.Invoice,
                targetId: invoice.InvoiceId,
                metadata: new
                {
                    invoiceNumber = invoice.InvoiceNumber,
                    amount = invoice.AmountDue,
                    source = "manual",
                    reference = dto.Reference
                });

            // Extend subscription — best-effort
            try
            {
                await _subscriptions.ExtendSubscriptionAsync(invoice.SubscriptionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Subscription extension failed for invoice {invoiceId}: {ex.Message}");
            }

            return await InvoiceToResponseAsync(invoiceId);
        }

        /// <summary>
        /// Called by Payments when webhook confirms success. No audit here — the
        /// payment module logs PAYMENT_SUCCESSFUL, which is the meaningful event.
        /// </summary>
        public async Task<object> ApplyPaymentSuccessAsync(int invoiceId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (invoice.Status == InvoiceStatus.PAID)
                return await InvoiceToResponseAsync(invoiceId);

            if (invoice.Status == InvoiceStatus.VOID)
                throw new InvalidOperationException("Cannot pay a voided invoice");

            invoice.Status = InvoiceStatus.PAID;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            try
            {
                await _subscriptions.ExtendSubscriptionAsync(invoice.SubscriptionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Subscription extension failed for invoice {invoiceId}: {ex.Message}");
            }

            return await InvoiceToResponseAsync(invoiceId);
        }

        // ─── HELPERS (unchanged) ────────────────────────────

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            const int maxAttempts = 5;
            var year = DateTime.UtcNow.Year;
            var prefix = $"INV-{year}-";

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var lastInvoiceNumber = await _db.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith(prefix))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .Select(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync();

                int nextSeq = 1;
                if (!string.IsNullOrEmpty(lastInvoiceNumber))
                {
                    var seqPart = lastInvoiceNumber.Substring(prefix.Length);
                    if (int.TryParse(seqPart, out var parsed))
                        nextSeq = parsed + 1;
                }

                var candidate = TokenHelper.GenerateInvoiceNumber(nextSeq);

                var exists = await _db.Invoices.AnyAsync(i => i.InvoiceNumber == candidate);
                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Could not generate a unique invoice number after multiple attempts. Please retry.");
        }

        private async Task<List<object>> BuildResponseListAsync(List<int> invoiceIds)
        {
            if (invoiceIds.Count == 0)
                return new List<object>();

            var invoices = await _db.Invoices
                .Include(i => i.Academy)
                .Include(i => i.Plan)
                .Where(i => invoiceIds.Contains(i.InvoiceId))
                .ToListAsync();

            var ordered = invoiceIds
                .Select(id => invoices.FirstOrDefault(i => i.InvoiceId == id))
                .Where(i => i != null)
                .ToList();

            return ordered.Select(i => InvoiceToResponse(i!)).Cast<object>().ToList();
        }

        private async Task<object> InvoiceToResponseAsync(int invoiceId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Academy)
                .Include(i => i.Plan)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            return InvoiceToResponse(invoice);
        }

        private static object InvoiceToResponse(Invoice i)
        {
            var daysUntilDue = DateHelper.DaysUntil(i.DueDate);

            return new
            {
                invoiceId = i.InvoiceId,
                invoiceNumber = i.InvoiceNumber,
                subscriptionId = i.SubscriptionId,
                academyId = i.AcademyId,
                academyName = i.Academy?.Name,
                planId = i.PlanId,
                planName = i.Plan?.Name,
                amountDue = i.AmountDue,
                currency = i.Currency,
                status = i.Status.ToString(),
                issuedAt = i.IssuedAt,
                dueDate = i.DueDate,
                paidAt = i.PaidAt,
                daysUntilDue = i.Status == InvoiceStatus.PAID ? (int?)null : daysUntilDue,
                periodStart = i.PeriodStart,
                periodEnd = i.PeriodEnd,
                voidedAt = i.VoidedAt,
                voidedReason = i.VoidedReason,
                createdAt = i.CreatedAt,
                updatedAt = i.UpdatedAt
            };
        }
    }
}