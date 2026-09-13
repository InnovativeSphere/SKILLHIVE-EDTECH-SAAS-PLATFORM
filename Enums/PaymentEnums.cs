namespace SkillHive.Enums
{
    public enum PaymentStatus
    {
        PENDING,
        SUCCESS,
        FAILED,
        REFUNDED
    }

    public enum PaymentPurpose
    {
        SUBSCRIPTION,
        COURSE_PURCHASE
    }

    public enum PaymentProvider
    {
        PAYSTACK
    }
}