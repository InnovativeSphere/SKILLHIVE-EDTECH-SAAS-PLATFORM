using System.Security.Cryptography;

namespace SkillHive.Common
{
    public static class TokenHelper
    {
        /// <summary>
        /// Generates a 6-digit numeric OTP using a cryptographically secure RNG.
        /// Used for academy owner verification.
        /// </summary>
        public static string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6"); // always 6 digits, zero-padded
        }

        /// <summary>
        /// Generates a URL-safe random token for email verification, password reset,
        /// or invite links.
        /// </summary>
        public static string GenerateVerificationToken(int byteLength = 32)
        {
            var bytes = new byte[byteLength];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>
        /// Generates a human-readable certificate verification code.
        /// Format: CERT-{YEAR}-{6 random alphanumeric chars}
        /// Example: CERT-2026-XK7M9P
        /// </summary>
        public static string GenerateCertificateCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I, O, 0, 1
            var chars = new char[6];
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[6];
            rng.GetBytes(bytes);

            for (int i = 0; i < 6; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }

            return $"CERT-{DateTime.UtcNow.Year}-{new string(chars)}";
        }

        /// <summary>
        /// Generates a unique payment reference for Paystack.
        /// Format: SKH-{timestamp}-{random}
        /// </summary>
        public static string GeneratePaymentReference()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"SKH-{timestamp}-{random}";
        }

        /// <summary>
        /// Generates a unique invoice number.
        /// Format: INV-{YEAR}-{5-digit sequence}
        /// Example: INV-2026-00042
        /// </summary>
        public static string GenerateInvoiceNumber(int sequence)
        {
            return $"INV-{DateTime.UtcNow.Year}-{sequence.ToString("D5")}";
        }
    }
}