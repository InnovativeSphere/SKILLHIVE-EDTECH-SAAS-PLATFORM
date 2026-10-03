using Microsoft.EntityFrameworkCore;
using SkillHive.Data;

namespace SkillHive.Features.Audit.Services
{
    public class SystemAdminService
    {
        private readonly AppDbContext _db;

        public SystemAdminService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Basic health check — verifies the DB connection works.
        /// Returns status + database state + uptime.
        /// </summary>
        public async Task<object> GetHealthAsync()
        {
            var database = "connected";
            try
            {
                // Cheapest possible DB round-trip
                await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            }
            catch
            {
                database = "disconnected";
            }

            return new
            {
                status = database == "connected" ? "ok" : "error",
                database,
                uptime = Environment.TickCount64 / 1000,
                timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// System information — version, environment, uptime.
        /// Safe to expose to superadmin only.
        /// </summary>
        public object GetSystemInfo()
        {
            return new
            {
                appName = "SkillHive",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                uptime = Environment.TickCount64 / 1000,
                timestamp = DateTime.UtcNow
            };
        }

        
    }
}