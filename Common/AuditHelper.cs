namespace SkillHive.Common
{
    /// <summary>
    /// String constants for audit actions.
    /// Using constants instead of raw strings prevents typos like
    /// "COURSE_PUBLISH" vs "COURSE_PUBLISHED" that would break queries.
    /// Not an enum — new actions can be added without a migration.
    /// </summary>
    public static class AuditActions
    {
        // ─── Auth ──────────────────────────────────────────
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string AcademyRegistered = "ACADEMY_REGISTERED";
        public const string StudentRegistered = "STUDENT_REGISTERED";
        public const string EmailVerified = "EMAIL_VERIFIED";
        public const string OtpVerified = "OTP_VERIFIED";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string InviteAccepted = "INVITE_ACCEPTED";

        // ─── Staff ─────────────────────────────────────────
        public const string StaffInvited = "STAFF_INVITED";
        public const string StaffUpdated = "STAFF_UPDATED";
        public const string StaffDeactivated = "STAFF_DEACTIVATED";
        public const string StaffReactivated = "STAFF_REACTIVATED";

        // ─── Course ────────────────────────────────────────
        public const string CoursePublished = "COURSE_PUBLISHED";
        public const string CourseRejected = "COURSE_REJECTED";
        public const string CourseArchived = "COURSE_ARCHIVED";

        // ─── Certificate ───────────────────────────────────
        public const string CertificateIssued = "CERTIFICATE_ISSUED";
        public const string CertificateRevoked = "CERTIFICATE_REVOKED";
        public const string CertificateReissued = "CERTIFICATE_REISSUED";

        // ─── Subscription ──────────────────────────────────
        public const string SubscriptionAssigned = "SUBSCRIPTION_ASSIGNED";
        public const string SubscriptionPlanChanged = "SUBSCRIPTION_PLAN_CHANGED";
        public const string SubscriptionCancelled = "SUBSCRIPTION_CANCELLED";
        public const string SubscriptionSuspended = "SUBSCRIPTION_SUSPENDED";
        public const string SubscriptionReactivated = "SUBSCRIPTION_REACTIVATED";

        // ─── Invoice ───────────────────────────────────────
        public const string InvoiceCreated = "INVOICE_CREATED";
        public const string InvoiceVoided = "INVOICE_VOIDED";
        public const string InvoicePaid = "INVOICE_PAID";

        // ─── Payment ───────────────────────────────────────
        public const string PaymentSuccessful = "PAYMENT_SUCCESSFUL";
        public const string PaymentFailed = "PAYMENT_FAILED";

        // ─── Academy ───────────────────────────────────────
        public const string AcademyProfileUpdated = "ACADEMY_PROFILE_UPDATED";
    }

    /// <summary>
    /// String constants for audit target types.
    /// Used to filter: "show me everything that happened to certificate 42".
    /// </summary>
    public static class AuditTargetTypes
    {
        public const string User = "USER";
        public const string Academy = "ACADEMY";
        public const string Staff = "STAFF";
        public const string Course = "COURSE";
        public const string Lesson = "LESSON";
        public const string Certificate = "CERTIFICATE";
        public const string Subscription = "SUBSCRIPTION";
        public const string Invoice = "INVOICE";
        public const string Payment = "PAYMENT";
    }
}