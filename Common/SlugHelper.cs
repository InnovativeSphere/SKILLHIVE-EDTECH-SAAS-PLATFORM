using System.Text;
using System.Text.RegularExpressions;

namespace SkillHive.Common
{
    public static class SlugHelper
    {
        /// <summary>
        /// Converts any text into a URL-friendly slug.
        /// "Baking with Amina!" -> "baking-with-amina"
        /// </summary>
        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Normalize accented characters (é -> e, ç -> c, etc.)
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // Replace non-alphanumeric with dashes
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", "");

            // Collapse whitespace and dashes
            cleaned = Regex.Replace(cleaned, @"[\s-]+", "-");

            // Trim leading/trailing dashes
            cleaned = cleaned.Trim('-');

            return cleaned;
        }

        /// <summary>
        /// Ensures a slug is unique by appending -1, -2, etc. until it doesn't collide.
        /// Pass a callback that checks whether the slug already exists.
        /// </summary>
        public static async Task<string> EnsureUniqueSlugAsync(
            string baseSlug,
            Func<string, Task<bool>> existsAsync)
        {
            if (string.IsNullOrWhiteSpace(baseSlug))
                return "item";

            var candidate = baseSlug;
            var counter = 1;

            while (await existsAsync(candidate))
            {
                candidate = $"{baseSlug}-{counter}";
                counter++;
            }

            return candidate;
        }
    }
}