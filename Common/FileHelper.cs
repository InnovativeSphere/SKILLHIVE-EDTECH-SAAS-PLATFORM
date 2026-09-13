using SkillHive.Enums;
namespace SkillHive.Common
{
    public static class FileHelper
    {
        // Allowed extensions for material uploads
        private static readonly HashSet<string> AllowedExtensions = new()
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx",
            ".xls", ".xlsx", ".txt", ".md",
            ".jpg", ".jpeg", ".png", ".webp", ".gif",
            ".mp4", ".webm", ".mov"
        };

        // MIME types we accept. Kept broad but safe.
        private static readonly HashSet<string> AllowedMimeTypes = new()
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain",
            "text/markdown",
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
            "video/mp4",
            "video/webm",
            "video/quicktime"
        };

        /// <summary>
        /// Validates a file by extension and MIME type.
        /// Returns false if either is not in the allowed list.
        /// </summary>
        public static bool IsAllowedFile(string fileName, string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return false;

            if (!string.IsNullOrWhiteSpace(mimeType) && !AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
                return false;

            return true;
        }

        /// <summary>
        /// Formats a byte count for display.
        /// 2_411_724 -> "2.3 MB"
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }

        /// <summary>
        /// Generates a unique file name to prevent collisions.
        /// "lesson-notes.pdf" -> "20260912143012_a7c3_lesson-notes.pdf"
        /// </summary>
        public static string GenerateUniqueFileName(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                return Guid.NewGuid().ToString("N");

            var extension = Path.GetExtension(originalFileName);
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);

            // Sanitize base name
            var safeBase = string.Join("-",
                baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
            ).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(safeBase))
                safeBase = "file";

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = Guid.NewGuid().ToString("N").Substring(0, 4);

            return $"{timestamp}_{random}_{safeBase}{extension}".ToLowerInvariant();
        }

        /// <summary>
        /// Returns a file type category based on extension, for the Material model's FileType field.
        /// </summary>
        public static FileType GetFileTypeCategory(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();

    return extension switch
    {
        ".pdf" => FileType.PDF,
        ".doc" or ".docx" => FileType.DOCX,
        ".ppt" or ".pptx" => FileType.PPTX,
        ".xls" or ".xlsx" => FileType.EXCEL,
        ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" => FileType.IMAGE,
        ".mp4" or ".webm" or ".mov" => FileType.VIDEO,
        ".txt" or ".md" => FileType.TEXT,
        _ => FileType.OTHER
    };
}
    }
}