namespace SkillHive.Enums
{
    public enum NotificationType
    {
        WELCOME,
        OTP_VERIFICATION,
        EMAIL_VERIFICATION,
        PASSWORD_RESET,
        STAFF_INVITE,
        COURSE_PUBLISHED,
        COURSE_REJECTED,
        ENROLLMENT_CONFIRMED,
        CERTIFICATE_ISSUED,
        CERTIFICATE_REISSUE_REQUEST,
        CERTIFICATE_REISSUE_APPROVED,
        PAYMENT_RECEIVED,
        INVOICE_ISSUED,
        INVOICE_OVERDUE,
        SUBSCRIPTION_EXPIRING,
        GENERAL
    }

    public enum VerificationTokenType
    {
        OTP,
        EMAIL_VERIFICATION,
        PASSWORD_RESET,
        INVITE
    }
}