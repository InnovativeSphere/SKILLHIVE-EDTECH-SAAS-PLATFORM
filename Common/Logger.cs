using System.Text.Json;

namespace SkillHive.Common
{
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly string _logFile;
        private static readonly object _lock = new();

        public Logger(IConfiguration config)
        {
            _logDirectory = config["Logging:File:Directory"] ?? "logs";
            _logFile = Path.Combine(_logDirectory, $"skillhive-{DateTime.UtcNow:yyyy-MM-dd}.log");

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public void Info(string message, object? context = null)
            => Write("INFO", message, context);

        public void Warning(string message, object? context = null)
            => Write("WARN", message, context);

        public void Error(string message, Exception? ex = null, object? context = null)
            => Write("ERROR", message, context, ex);

        public void Debug(string message, object? context = null)
            => Write("DEBUG", message, context);

        private void Write(string level, string message, object? context = null, Exception? ex = null)
        {
            var entry = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                level,
                message,
                context,
                exception = ex?.ToString()
            };

            var line = JsonSerializer.Serialize(entry);

            lock (_lock)
            {
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }

            // Also echo to console so we see it live
            Console.WriteLine($"[{level}] {message}");
            if (ex != null)
                Console.WriteLine(ex.Message);
        }
    }
}