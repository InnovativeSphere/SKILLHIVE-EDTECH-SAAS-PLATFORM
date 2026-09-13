namespace SkillHive.Common
{
    public static class Utils
    {
        /// <summary>
        /// Hashes a plain-text password using BCrypt.
        /// Fully qualified to avoid namespace collision with the BCrypt class.
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a plain-text password against a BCrypt hash.
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// Converts a string to Title Case.
        /// "salim sambo" -> "Salim Sambo"
        /// </summary>
        public static string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(' ', words);
        }

        /// <summary>
        /// Trims, collapses whitespace, and strips control characters.
        /// Used on user-supplied strings before storage.
        /// </summary>
        public static string SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var cleaned = new string(input.Where(c => !char.IsControl(c)).ToArray());
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts).Trim();
        }

        /// <summary>
        /// Truncates a string to a maximum length, appending ellipsis if cut.
        /// Used for previews in list responses.
        /// </summary>
        public static string Truncate(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input ?? string.Empty;

            return input.Substring(0, maxLength).TrimEnd() + "...";
        }

        /// <summary>
        /// Masks an email address for logs and audit trails.
        /// "salim@example.com" -> "s****@example.com"
        /// </summary>
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
                return "***" + email.Substring(Math.Max(atIndex, 0));

            var first = email[0];
            var domain = email.Substring(atIndex);
            return $"{first}****{domain}";
        }

        /// <summary>
        /// Masks a phone number for logs and audit trails.
        /// "09024842586" -> "0902****586"
        /// </summary>
        public static string MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
                return "***";

            var start = phone.Substring(0, 4);
            var end = phone.Substring(phone.Length - 3);
            return $"{start}****{end}";
        }
    }
}