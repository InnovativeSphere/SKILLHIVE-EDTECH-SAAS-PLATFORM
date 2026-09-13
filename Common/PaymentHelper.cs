using System.Security.Cryptography;
using System.Text;

namespace SkillHive.Common
{
    public static class PaymentHelper
    {
        /// <summary>
        /// Converts a Naira amount to Kobo (Paystack's base unit).
        /// ₦10,000.50 -> 1000050
        /// </summary>
        public static long ConvertNairaToKobo(decimal amountInNaira)
        {
            return (long)Math.Round(amountInNaira * 100);
        }

        /// <summary>
        /// Converts Kobo back to Naira.
        /// 1000050 -> ₦10,000.50
        /// </summary>
        public static decimal ConvertKoboToNaira(long amountInKobo)
        {
            return amountInKobo / 100m;
        }

        /// <summary>
        /// Verifies a Paystack webhook signature using HMAC SHA512.
        /// The raw request body (as bytes) and our secret key are used
        /// to compute the expected signature. It's compared against the
        /// x-paystack-signature header.
        /// </summary>
        public static bool VerifyPaystackSignature(byte[] rawBody, string signature, string secretKey)
        {
            if (rawBody == null || rawBody.Length == 0)
                return false;

            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secretKey))
                return false;

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
            var computedHash = hmac.ComputeHash(rawBody);
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(signature.ToLowerInvariant())
            );
        }

        /// <summary>
        /// Determines if a payment status is final (no further changes expected).
        /// Used by the Payments module to know when a transaction is settled.
        /// </summary>
        public static bool IsFinalStatus(string status)
        {
            return status switch
            {
                "SUCCESS" => true,
                "FAILED" => true,
                "REFUNDED" => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if a payment status represents money received.
        /// </summary>
        public static bool IsPaidStatus(string status)
        {
            return status == "SUCCESS";
        }
    }
}