using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Invoices.DTOs;
using SkillHive.Features.Subscriptions.Services;
using SkillHive.Models;

namespace SkillHive.Features.Invoices.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _db;
        private readonly SubscriptionService _subscriptions;

        public InvoiceService(AppDbContext db, SubscriptionService subscriptions)
        {
            _db = db;
            _subscriptions = subscriptions;
        }

        // ─── SUPERADMIN — MANUAL CREATION ───────────────────

        /// <summary>
        /// Superadmin creates an invoice manually with an explicit amount and period.
        /// Used for corrections, one-off billing, or invoices paid outside Paystack.
        /// </summary>
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

            return await InvoiceToResponseAsync(invoice.InvoiceId);
        }

        // ─── SUPERADMIN — AUTOMATED GENERATION ──────────────

        /// <summary>
        /// Generates the next-period invoice for a subscription.
        /// Amount comes from plan.Price; period comes from subscription.EndDate + plan.Interval.
        /// Superadmin triggers this manually (a scheduler would call it in production).
        /// </summary>
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

            // Don't double-issue: if an UNPAID invoice already exists, stop
            var existingUnpaid = await _db.Invoices
                .AnyAsync(i => i.SubscriptionId == subscriptionId
                               && (i.Status == InvoiceStatus.UNPAID || i.Status == InvoiceStatus.DRAFT));

            if (existingUnpaid)
                throw new InvalidOperationException(
                    "Subscription already has an unpaid invoice. Settle it or void it first.");

            var now = DateTime.UtcNow;
            var periodStart = subscription.EndDate > now ? subscription.EndDate : now;
            var periodEnd = DateHelper.CalculateSubscriptionEndDate(periodStart, subscription.Plan.Interval);
            var dueDate = periodStart; // bill immediately for the upcoming period

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

            return await InvoiceToResponseAsync(invoice.InvoiceId);
        }

        // ─── READS ──────────────────────────────────────────

        /// <summary>
        /// Single invoice. Scoped: superadmin sees all, owner sees only their academy's.
        /// </summary>
        public async Task<object> GetInvoiceAsync(
            int invoiceId, UserRole role, int? requesterAcademyId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            // Owner can only see their own academy's invoices
            if (role != UserRole.SUPER_ADMIN && invoice.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You do not have permission to view this invoice");

            return await InvoiceToResponseAsync(invoiceId);
        }

        /// <summary>
        /// All invoices for the caller's own academy.
        /// </summary>
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

        /// <summary>
        /// All invoices for a specific subscription.
        /// Owner can only query their own subscription's invoices.
        /// </summary>
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

        /// <summary>
        /// Platform-wide listing — superadmin only.
        /// Optional filters by status and academy.
        /// </summary>
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

        /// <summary>
        /// Void an invoice. Blocked if already PAID or VOIDED.
        /// Invoice is preserved; status changes and reason is recorded.
        /// </summary>
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

            return await InvoiceToResponseAsync(invoiceId);
        }

        /// <summary>
        /// Mark invoice PAID. Extends the related subscription.
        /// Called by superadmin for manual payments, or by Payments module
        /// when a Paystack webhook confirms payment.
        /// </summary>
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

            // Extend the subscription. Best-effort — if it fails, invoice stays PAID;
            // the subscription can be manually corrected by superadmin.
            try
            {
                await _subscriptions.ExtendSubscriptionAsync(invoice.SubscriptionId);
            }
            catch (Exception ex)
            {
                // Log but don't fail the invoice payment — the payment already happened
                Console.WriteLine($"Subscription extension failed for invoice {invoiceId}: {ex.Message}");
            }

            return await InvoiceToResponseAsync(invoiceId);
        }

        // ─── HELPERS ─────────────────────────────────────────

        /// <summary>
        /// Generates the next invoice number for the current year.
        /// Format: INV-{YEAR}-{5-digit-seq}. Resets each year.
        /// Retries up to 5 times to avoid collisions with concurrent inserts.
        /// </summary>
        private async Task<string> GenerateInvoiceNumberAsync()
        {
            const int maxAttempts = 5;
            var year = DateTime.UtcNow.Year;
            var prefix = $"INV-{year}-";

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Find the highest sequence number for this year
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

                // Safety check — should never collide given the ordering above,
                // but protects against race conditions
                var exists = await _db.Invoices.AnyAsync(i => i.InvoiceNumber == candidate);
                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Could not generate a unique invoice number after multiple attempts. Please retry.");
        }

        /// <summary>
        /// Builds the response list for a batch of invoice IDs. Preserves order.
        /// </summary>
        private async Task<List<object>> BuildResponseListAsync(List<int> invoiceIds)
        {
            if (invoiceIds.Count == 0)
                return new List<object>();

            var invoices = await _db.Invoices
                .Include(i => i.Academy)
                .Include(i => i.Plan)
                .Where(i => invoiceIds.Contains(i.InvoiceId))
                .ToListAsync();

            // Re-order to match the input (EF doesn't guarantee order after IN queries)
            var ordered = invoiceIds
                .Select(id => invoices.FirstOrDefault(i => i.InvoiceId == id))
                .Where(i => i != null)
                .ToList();

            return ordered.Select(i => InvoiceToResponse(i!)).Cast<object>().ToList();
        }

        /// <summary>
        /// Single-invoice response builder — loads nav properties then formats.
        /// </summary>
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
            var now = DateTime.UtcNow;
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