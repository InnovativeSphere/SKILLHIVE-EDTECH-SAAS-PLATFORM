using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Email.Services;
using SkillHive.Features.Enrollments.Services;
using SkillHive.Features.Invoices.Services;
using SkillHive.Features.Notifications.Services;
using SkillHive.Features.Payments.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Payments.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _db;
        private readonly InvoiceService _invoices;
        private readonly EnrollmentService _enrollments;
        private readonly NotificationService _notifications;
        private readonly EmailService _email;
        private readonly Logger _logger;
        private readonly IConfiguration _config;

        public PaymentService(
            AppDbContext db,
            InvoiceService invoices,
            EnrollmentService enrollments,
            NotificationService notifications,
            EmailService email,
            Logger logger,
            IConfiguration config)
        {
            _db = db;
            _invoices = invoices;
            _enrollments = enrollments;
            _notifications = notifications;
            _email = email;
            _logger = logger;
            _config = config;
        }

        // ─── INITIALIZE — SUBSCRIPTION ──────────────────────

        /// <summary>
        /// Owner pays for a subscription invoice.
        /// Derives amount, email, and academy from the invoice — never from the client.
        /// </summary>
        public async Task<object> InitializeSubscriptionPaymentAsync(
            InitializeSubscriptionPaymentDto dto, int userId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Academy)
                .Include(i => i.Plan)
                .FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (invoice.Status != InvoiceStatus.UNPAID)
                throw new InvalidOperationException($"Cannot pay an invoice with status {invoice.Status}");

            // The user initiating the payment must own the academy that owes the invoice
            var requester = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (requester == null)
                throw new InvalidOperationException("User not found");

            if (requester.AcademyId != invoice.AcademyId)
                throw new UnauthorizedAccessException("You can only pay invoices for your own academy");

            // Fetch the owner's email for Paystack
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.AcademyId == invoice.AcademyId
                                                                  && u.Role == UserRole.ACADEMY_OWNER);

            if (owner == null || string.IsNullOrWhiteSpace(owner.Email))
                throw new InvalidOperationException("Academy owner email not found");

            var amountNaira = invoice.AmountDue;
            var amountKobo = PaymentHelper.ConvertNairaToKobo(amountNaira);
            var reference = TokenHelper.GeneratePaymentReference();

            // Persist the PENDING transaction BEFORE calling Paystack.
            // If Paystack fails, we have a record of the attempt.
            var transaction = new PaymentTransaction
            {
                Reference = reference,
                Amount = amountNaira,
                Currency = invoice.Currency,
                Status = PaymentStatus.PENDING,
                Provider = PaymentProvider.PAYSTACK,
                Purpose = PaymentPurpose.SUBSCRIPTION,
                AcademyId = invoice.AcademyId,
                InvoiceId = invoice.InvoiceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            // Call Paystack
            var initResponse = await CallPaystackInitializeAsync(
                email: owner.Email,
                amountKobo: amountKobo,
                reference: reference,
                metadata: new
                {
                    purpose = "SUBSCRIPTION",
                    academyId = invoice.AcademyId,
                    invoiceId = invoice.InvoiceId,
                    userId = userId
                });

            if (!initResponse.IsSuccess || string.IsNullOrWhiteSpace(initResponse.AuthorizationUrl))
            {
                transaction.Status = PaymentStatus.FAILED;
                transaction.FailureReason = initResponse.ErrorMessage ?? "Paystack initialization failed";
                transaction.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                throw new InvalidOperationException(
                    initResponse.ErrorMessage ?? "Failed to initialize payment with Paystack");
            }

            transaction.ProviderPayload = initResponse.RawResponse;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                reference,
                authorizationUrl = initResponse.AuthorizationUrl,
                amountNaira,
                purpose = "SUBSCRIPTION",
                invoiceId = invoice.InvoiceId
            };
        }

        // ─── INITIALIZE — COURSE PURCHASE ───────────────────

        /// <summary>
        /// Student pays for a paid course.
        /// Derives amount from the course; student identity comes from JWT.
        /// </summary>
        public async Task<object> InitializeCoursePaymentAsync(
            InitializeCoursePaymentDto dto, int userId)
        {
            var course = await _db.Courses
                .Include(c => c.Academy)
                .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

            if (course == null)
                throw new InvalidOperationException("Course not found");

            if (course.Status != CourseStatus.PUBLISHED)
                throw new InvalidOperationException("Course is not available for purchase");

            if (course.Visibility == CourseVisibility.PRIVATE)
                throw new InvalidOperationException("Course is not available for purchase");

            if (!course.Academy.IsActive)
                throw new InvalidOperationException("Course is not available for purchase");

            if (course.IsFree)
                throw new InvalidOperationException("This course is free. Use the enrollment endpoint.");

            var effectivePrice = course.DiscountPrice ?? course.Price;
            if (effectivePrice == null || effectivePrice <= 0)
                throw new InvalidOperationException("Course price is not configured");

            var student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (student == null)
                throw new InvalidOperationException("Student not found");

            if (student.Role != UserRole.STUDENT)
                throw new InvalidOperationException("Only students can purchase courses");

            // Already enrolled?
            var alreadyEnrolled = await _db.Enrollments
                .AnyAsync(e => e.StudentId == userId
                               && e.CourseId == dto.CourseId
                               && (e.Status == EnrollmentStatus.ACTIVE || e.Status == EnrollmentStatus.COMPLETED));

            if (alreadyEnrolled)
                throw new InvalidOperationException("You are already enrolled in this course");

            // Prevent duplicate PENDING payments for the same course by the same student
            var pendingDuplicate = await _db.PaymentTransactions
                .AnyAsync(p => p.StudentId == userId
                               && p.CourseId == dto.CourseId
                               && p.Purpose == PaymentPurpose.COURSE_PURCHASE
                               && p.Status == PaymentStatus.PENDING);

            if (pendingDuplicate)
                throw new InvalidOperationException("You already have a pending payment for this course. Complete or cancel it first.");

            var amountNaira = effectivePrice.Value;
            var amountKobo = PaymentHelper.ConvertNairaToKobo(amountNaira);
            var reference = TokenHelper.GeneratePaymentReference();

            var transaction = new PaymentTransaction
            {
                Reference = reference,
                Amount = amountNaira,
                Currency = course.Currency,
                Status = PaymentStatus.PENDING,
                Provider = PaymentProvider.PAYSTACK,
                Purpose = PaymentPurpose.COURSE_PURCHASE,
                StudentId = userId,
                CourseId = course.CourseId,
                AcademyId = course.AcademyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            var initResponse = await CallPaystackInitializeAsync(
                email: student.Email,
                amountKobo: amountKobo,
                reference: reference,
                metadata: new
                {
                    purpose = "COURSE_PURCHASE",
                    courseId = course.CourseId,
                    studentId = userId,
                    academyId = course.AcademyId
                });

            if (!initResponse.IsSuccess || string.IsNullOrWhiteSpace(initResponse.AuthorizationUrl))
            {
                transaction.Status = PaymentStatus.FAILED;
                transaction.FailureReason = initResponse.ErrorMessage ?? "Paystack initialization failed";
                transaction.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                throw new InvalidOperationException(
                    initResponse.ErrorMessage ?? "Failed to initialize payment with Paystack");
            }

            transaction.ProviderPayload = initResponse.RawResponse;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                reference,
                authorizationUrl = initResponse.AuthorizationUrl,
                amountNaira,
                purpose = "COURSE_PURCHASE",
                courseId = course.CourseId,
                courseTitle = course.Title
            };
        }

        // ─── WEBHOOK ────────────────────────────────────────

        /// <summary>
        /// Receives Paystack webhook. Verifies signature, processes only charge.success,
        /// and is idempotent — safe to call multiple times for the same reference.
        /// Returns a status object (never throws) so Paystack gets a 200 and doesn't retry.
        /// </summary>
        public async Task<object> HandleWebhookAsync(string signature, byte[] rawBody)
        {
            var secretKey = _config["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.Error("Paystack secret key is not configured");
                return new { status = "error", message = "Payment provider not configured" };
            }

            if (!PaymentHelper.VerifyPaystackSignature(rawBody, signature, secretKey))
            {
                _logger.Warning("Paystack webhook signature verification failed");
                return new { status = "error", message = "Invalid signature" };
            }

            string payloadJson;
            try
            {
                payloadJson = Encoding.UTF8.GetString(rawBody);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to decode webhook body", ex);
                return new { status = "error", message = "Invalid body encoding" };
            }

            JsonElement root;
            try
            {
                root = JsonDocument.Parse(payloadJson).RootElement;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to parse webhook JSON", ex);
                return new { status = "error", message = "Invalid JSON" };
            }

            var eventType = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
            if (eventType != "charge.success")
                return new { status = "ignored", message = $"Event {eventType} not handled" };

            if (!root.TryGetProperty("data", out var data))
                return new { status = "error", message = "Missing data object" };

            var reference = data.TryGetProperty("reference", out var refProp) ? refProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(reference))
                return new { status = "error", message = "Missing reference" };

            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.Reference == reference);

            if (transaction == null)
            {
                _logger.Warning($"Webhook received for unknown reference: {reference}");
                return new { status = "error", message = "Transaction not found" };
            }

            // Idempotency — already processed?
            if (transaction.Status == PaymentStatus.SUCCESS)
                return new { status = "already_processed" };

            // Verify the amount matches — Paystack sends kobo
            if (data.TryGetProperty("amount", out var amountProp))
            {
                var receivedKobo = amountProp.GetInt64();
                var expectedKobo = PaymentHelper.ConvertNairaToKobo(transaction.Amount);

                if (receivedKobo != expectedKobo)
                {
                    transaction.Status = PaymentStatus.FAILED;
                    transaction.FailureReason = $"Amount mismatch: expected {expectedKobo} kobo, received {receivedKobo} kobo";
                    transaction.ProviderPayload = payloadJson;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    _logger.Error($"Amount mismatch for reference {reference}: expected {expectedKobo}, got {receivedKobo}");
                    return new { status = "error", message = "Amount mismatch" };
                }
            }

            // Extract fields from webhook
            var channel = data.TryGetProperty("channel", out var ch) ? ch.GetString() : null;
            var paidAtStr = data.TryGetProperty("paid_at", out var paid) ? paid.GetString() : null;
            var paidAt = !string.IsNullOrWhiteSpace(paidAtStr)
                ? DateTime.Parse(paidAtStr).ToUniversalTime()
                : DateTime.UtcNow;

            transaction.Status = PaymentStatus.SUCCESS;
            transaction.Channel = channel;
            transaction.PaidAt = paidAt;
            transaction.ProviderPayload = payloadJson;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await ProcessSuccessfulPaymentAsync(transaction);

            _logger.Info($"Webhook processed successfully for {reference}");
            return new { status = "success" };
        }

        // ─── VERIFY (fallback after redirect) ───────────────

        /// <summary>
        /// Frontend calls this after Paystack redirects the user back.
        /// Reconciles if the webhook hasn't fired yet — belt and braces.
        /// </summary>
        public async Task<object> VerifyPaymentAsync(VerifyPaymentDto dto, int userId)
        {
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.Reference == dto.Reference);

            if (transaction == null)
                throw new InvalidOperationException("Payment reference not found");

            // Ownership check — the caller must own this transaction
            if (transaction.StudentId.HasValue && transaction.StudentId.Value != userId)
            {
                // Course purchase — check the student
                throw new UnauthorizedAccessException("You can only verify your own payments");
            }
            if (transaction.AcademyId.HasValue)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.AcademyId != transaction.AcademyId)
                    throw new UnauthorizedAccessException("You can only verify your own academy's payments");
            }

            // Already processed?
            if (transaction.Status == PaymentStatus.SUCCESS)
                return BuildVerifyResponse(transaction);

            if (transaction.Status == PaymentStatus.FAILED || transaction.Status == PaymentStatus.REFUNDED)
                return BuildVerifyResponse(transaction);

            // Still pending — ask Paystack directly
            var verifyResponse = await CallPaystackVerifyAsync(transaction.Reference);
            if (!verifyResponse.IsSuccess)
            {
                // Paystack couldn't verify — return current pending state
                return BuildVerifyResponse(transaction);
            }

            if (!verifyResponse.Paid)
            {
                // Paystack says it's not successful
                return BuildVerifyResponse(transaction);
            }

            // Webhook didn't fire (or is delayed) — reconcile now
            transaction.Status = PaymentStatus.SUCCESS;
            transaction.Channel = verifyResponse.Channel;
            transaction.PaidAt = verifyResponse.PaidAt ?? DateTime.UtcNow;
            transaction.ProviderPayload = verifyResponse.RawResponse;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await ProcessSuccessfulPaymentAsync(transaction);

            return BuildVerifyResponse(transaction);
        }

        // ─── SHARED SUCCESS PROCESSING ──────────────────────

        /// <summary>
        /// Called from both webhook and verify.
        /// Branches on Purpose and triggers the downstream side effect
        /// (invoice paid + subscription extended, OR enrollment created).
        /// Idempotency is guaranteed upstream — this runs at most once per reference.
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(PaymentTransaction transaction)
        {
            try
            {
                if (transaction.Purpose == PaymentPurpose.SUBSCRIPTION && transaction.InvoiceId.HasValue)
                {
                    await _invoices.ApplyPaymentSuccessAsync(transaction.InvoiceId.Value);

                    // Receipt notification to owner
                    var owner = await _db.Users
                        .FirstOrDefaultAsync(u => u.AcademyId == transaction.AcademyId
                                                  && u.Role == UserRole.ACADEMY_OWNER);

                    if (owner != null)
                    {
                        await _notifications.NotifyAsync(
                            owner.UserId,
                            NotificationType.PAYMENT_RECEIVED,
                            "Subscription payment received",
                            $"Your payment of ₦{transaction.Amount:N2} has been received. Your subscription is now active.",
                            sendEmail: true,
                            emailTemplate: "payment-received",
                            emailModel: new
                            {
                                FullName = owner.FullName,
                                Amount = transaction.Amount,
                                Currency = transaction.Currency,
                                Reference = transaction.Reference,
                                PaidAt = transaction.PaidAt ?? DateTime.UtcNow
                            });
                    }
                }
                else if (transaction.Purpose == PaymentPurpose.COURSE_PURCHASE
                         && transaction.StudentId.HasValue
                         && transaction.CourseId.HasValue)
                {
                    var enrollment = await _enrollments.CreatePaidEnrollmentAsync(
                        transaction.StudentId.Value,
                        transaction.CourseId.Value,
                        transaction.PaymentId);

                    // Receipt + enrollment confirmation to student
                    var student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == transaction.StudentId.Value);
                    var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == transaction.CourseId.Value);

                    if (student != null && course != null)
                    {
                        await _notifications.NotifyAsync(
                            student.UserId,
                            NotificationType.PAYMENT_RECEIVED,
                            "Course purchase successful",
                            $"You've successfully purchased {course.Title}. Happy learning!",
                            sendEmail: true,
                            emailTemplate: "payment-received",
                            emailModel: new
                            {
                                FullName = student.FullName,
                                Amount = transaction.Amount,
                                Currency = transaction.Currency,
                                Reference = transaction.Reference,
                                PaidAt = transaction.PaidAt ?? DateTime.UtcNow,
                                CourseTitle = course.Title
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                // Payment is already SUCCESS in DB. Downstream side effects are best-effort.
                // A scheduled reconciliation job (V2) can retry failures.
                _logger.Error(
                    $"Post-payment processing failed for reference {transaction.Reference}",
                    ex);
            }
        }

        // ─── PAYSTACK HTTP CALLS ────────────────────────────

        private class PaystackInitResponse
        {
            public bool IsSuccess { get; set; }
            public string? AuthorizationUrl { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RawResponse { get; set; }
        }

        private class PaystackVerifyResponse
        {
            public bool IsSuccess { get; set; }
            public bool Paid { get; set; }
            public string? Channel { get; set; }
            public DateTime? PaidAt { get; set; }
            public string? RawResponse { get; set; }
        }

        private async Task<PaystackInitResponse> CallPaystackInitializeAsync(
            string email, long amountKobo, string reference, object metadata)
        {
            var baseUrl = _config["Paystack:BaseUrl"] ?? "https://api.paystack.co";
            var secretKey = _config["Paystack:SecretKey"];

            var requestBody = new
            {
                email,
                amount = amountKobo,
                reference,
                metadata
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            try
            {
                var response = await client.PostAsync($"{baseUrl}/transaction/initialize", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                var status = root.TryGetProperty("status", out var s) && s.GetBoolean();
                if (!status)
                {
                    var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                    return new PaystackInitResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = msg,
                        RawResponse = responseBody
                    };
                }

                var data = root.GetProperty("data");
                var url = data.GetProperty("authorization_url").GetString();

                return new PaystackInitResponse
                {
                    IsSuccess = true,
                    AuthorizationUrl = url,
                    RawResponse = responseBody
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Paystack initialize call failed", ex);
                return new PaystackInitResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<PaystackVerifyResponse> CallPaystackVerifyAsync(string reference)
        {
            var baseUrl = _config["Paystack:BaseUrl"] ?? "https://api.paystack.co";
            var secretKey = _config["Paystack:SecretKey"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            try
            {
                var response = await client.GetAsync($"{baseUrl}/transaction/verify/{reference}");
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                var status = root.TryGetProperty("status", out var s) && s.GetBoolean();
                if (!status)
                {
                    return new PaystackVerifyResponse { IsSuccess = false, RawResponse = responseBody };
                }

                var data = root.GetProperty("data");
                var txnStatus = data.TryGetProperty("status", out var ts) ? ts.GetString() : null;
                var paid = txnStatus == "success";

                string? channel = data.TryGetProperty("channel", out var ch) ? ch.GetString() : null;
                DateTime? paidAt = null;
                if (data.TryGetProperty("paid_at", out var pa) && pa.ValueKind == JsonValueKind.String)
                {
                    var paidAtStr = pa.GetString();
                    if (!string.IsNullOrWhiteSpace(paidAtStr))
                        paidAt = DateTime.Parse(paidAtStr).ToUniversalTime();
                }

                return new PaystackVerifyResponse
                {
                    IsSuccess = true,
                    Paid = paid,
                    Channel = channel,
                    PaidAt = paidAt,
                    RawResponse = responseBody
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Paystack verify call failed", ex);
                return new PaystackVerifyResponse { IsSuccess = false };
            }
        }

        // ─── RESPONSE BUILDER ───────────────────────────────

        private static object BuildVerifyResponse(PaymentTransaction t)
        {
            return new
            {
                paymentId = t.PaymentId,
                reference = t.Reference,
                amount = t.Amount,
                currency = t.Currency,
                status = t.Status.ToString(),
                purpose = t.Purpose.ToString(),
                channel = t.Channel,
                paidAt = t.PaidAt,
                invoiceId = t.InvoiceId,
                courseId = t.CourseId,
                createdAt = t.CreatedAt
            };
        }
    }
}