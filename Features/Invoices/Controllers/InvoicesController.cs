using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Invoices.DTOs;
using SkillHive.Features.Invoices.Services;

namespace SkillHive.Features.Invoices.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoicesController : ControllerBase
    {
        private readonly InvoiceService _invoiceService;

        public InvoicesController(InvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // ─── SUPERADMIN — MANUAL + AUTOMATED CREATION ──────

        /// <summary>
        /// Superadmin — create an invoice manually with an explicit amount.
        /// Use for corrections, one-off billing, or payments received outside Paystack.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> CreateManual([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _invoiceService.CreateManualInvoiceAsync(dto, userId);
                return ApiResponse.Created(result, "Invoice created");
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
        /// Superadmin — generate the next-period invoice from plan price.
        /// Blocked if subscription already has an unpaid invoice.
        /// </summary>
        [HttpPost("generate/{subscriptionId:int}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GenerateNext(int subscriptionId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _invoiceService.GenerateNextInvoiceAsync(subscriptionId, userId);
                return ApiResponse.Created(result, "Invoice generated");
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

        // ─── READS ──────────────────────────────────────────
        // Literal routes declared before parameter routes for readability.
        // ASP.NET Core's matcher handles precedence correctly regardless.

        /// <summary>
        /// Owner — list all invoices for your own academy.
        /// Superadmin should use /all instead.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null)
                    return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _invoiceService.ListMyAcademyInvoicesAsync(academyId.Value);
                return ApiResponse.Success(result);
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
        /// Superadmin — platform-wide invoice listing with optional filters.
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? academyId)
        {
            try
            {
                InvoiceStatus? parsedStatus = null;
                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<InvoiceStatus>(status.ToUpperInvariant(), out var s))
                {
                    parsedStatus = s;
                }

                var result = await _invoiceService.ListAllInvoicesAsync(parsedStatus, academyId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        /// <summary>
        /// Owner or superadmin — all invoices for a specific subscription.
        /// Service enforces: owner can only query their own academy's subscriptions.
        /// </summary>
        [HttpGet("subscription/{subscriptionId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> GetForSubscription(int subscriptionId)
        {
            try
            {
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _invoiceService.ListInvoicesForSubscriptionAsync(
                    subscriptionId, role, requesterAcademyId);
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

        /// <summary>
        /// Owner or superadmin — single invoice by ID.
        /// Owner can only fetch their own academy's invoices.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ACADEMY_OWNER")]
        public async Task<IActionResult> GetOne(int id)
        {
            try
            {
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var requesterAcademyId = JwtHelper.GetAcademyId(User);

                var result = await _invoiceService.GetInvoiceAsync(id, role, requesterAcademyId);
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

        // ─── SUPERADMIN — LIFECYCLE ACTIONS ─────────────────

        /// <summary>
        /// Superadmin — void an unpaid invoice. Blocked if already PAID or VOIDED.
        /// Invoice is preserved with status = VOID and a recorded reason.
        /// </summary>
        [HttpPost("{id:int}/void")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> Void(int id, [FromBody] VoidInvoiceDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _invoiceService.VoidInvoiceAsync(id, dto, userId);
                return ApiResponse.Success(result, "Invoice voided");
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
        /// Superadmin — mark an invoice PAID and extend the subscription.
        /// Called manually for cash/transfer payments, or by Payments module on webhook.
        /// </summary>
        [HttpPost("{id:int}/mark-paid")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkInvoicePaidDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _invoiceService.MarkPaidAsync(id, dto, userId);
                return ApiResponse.Success(result, "Invoice marked as paid");
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