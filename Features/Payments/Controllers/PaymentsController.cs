using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Payments.DTOs;
using SkillHive.Features.Payments.Services;

namespace SkillHive.Features.Payments.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentsController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ─── INITIALIZE — SUBSCRIPTION ──────────────────────

        /// <summary>
        /// Owner initializes a subscription payment.
        /// Amount is derived from the invoice — never from the client.
        /// Returns a Paystack authorization URL for redirect.
        /// </summary>
        [HttpPost("subscription/initialize")]
        [Authorize(Roles = "ACADEMY_OWNER")]
        public async Task<IActionResult> InitializeSubscription(
            [FromBody] InitializeSubscriptionPaymentDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _paymentService.InitializeSubscriptionPaymentAsync(dto, userId);
                return ApiResponse.Created(result, "Payment initialized");
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

        // ─── INITIALIZE — COURSE PURCHASE ───────────────────

        /// <summary>
        /// Student initializes a course purchase.
        /// Amount is derived from the course — never from the client.
        /// Returns a Paystack authorization URL for redirect.
        /// </summary>
        [HttpPost("course/initialize")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> InitializeCourse(
            [FromBody] InitializeCoursePaymentDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _paymentService.InitializeCoursePaymentAsync(dto, userId);
                return ApiResponse.Created(result, "Payment initialized");
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

        // ─── VERIFY (frontend callback) ─────────────────────

        /// <summary>
        /// Frontend calls this after Paystack redirects the user back.
        /// Reconciles with Paystack if the webhook hasn't fired yet.
        /// Requires auth — caller must own the transaction.
        /// </summary>
        [HttpPost("verify")]
        [Authorize]
        public async Task<IActionResult> Verify([FromBody] VerifyPaymentDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _paymentService.VerifyPaymentAsync(dto, userId);
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

        // ─── WEBHOOK ────────────────────────────────────────

        /// <summary>
        /// Paystack webhook receiver. No auth — verified by signature instead.
        /// Body is read as raw bytes for HMAC-SHA512 signature verification.
        /// ALWAYS returns 200 so Paystack doesn't retry (unless body is unreadable).
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                // Read raw body
                Request.EnableBuffering();
                byte[] rawBody;
                using (var ms = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(ms);
                    rawBody = ms.ToArray();
                }

                // Reset stream position in case anything downstream reads it
                Request.Body.Position = 0;

                var signature = Request.Headers["x-paystack-signature"].ToString();

                if (string.IsNullOrWhiteSpace(signature))
                {
                    // No signature → not from Paystack → respond OK to prevent retries
                    // (we don't want Paystack retrying and confusing us)
                    return Ok(new { status = "error", message = "Missing signature" });
                }

                var result = await _paymentService.HandleWebhookAsync(signature, rawBody);

                // Always 200 — Paystack retries on non-2xx
                return Ok(result);
            }
            catch (Exception)
            {
                // Even on internal error, return 200 — the service logs the issue.
                // Returning 500 causes Paystack to retry indefinitely.
                return Ok(new { status = "error", message = "Processing failed" });
            }
        }
    }
}