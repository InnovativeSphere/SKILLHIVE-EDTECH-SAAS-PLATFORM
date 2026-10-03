using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Models;
using SkillHive.Common;

namespace SkillHive.Features.Audit.Services
{
    public class AuditService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContext;

        public AuditService(AppDbContext db, IHttpContextAccessor httpContext)
        {
            _db = db;
            _httpContext = httpContext;
        }

        // ─── WRITE ──────────────────────────────────────────

        /// <summary>
        /// Appends an audit log entry.
        /// NEVER throws — audit logging must never break the calling flow.
        /// If it fails (DB down, serialization error), it swallows the exception
        /// and prints to console. The caller proceeds regardless.
        /// </summary>
        public async Task LogAsync(
            int userId,
            int? academyId,
            string action,
            string? targetType = null,
            int? targetId = null,
            object? metadata = null)
        {
            try
            {
                string? metadataJson = null;
                if (metadata != null)
                {
                    metadataJson = JsonSerializer.Serialize(metadata);
                }

                var ip = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

                var log = new AuditLog
                {
                    UserId = userId,
                    AcademyId = academyId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId,
                    Metadata = metadataJson,
                    IpAddress = ip,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit failures are not critical. Log to console for diagnostics
                // and let the caller continue.
                Console.WriteLine($"[AUDIT FAILURE] action={action} userId={userId} error={ex.Message}");
            }
        }

        // ─── READ ───────────────────────────────────────────

        /// <summary>
        /// Superadmin query — platform-wide with optional filters.
        /// </summary>
        public async Task<object> GetAllAsync(
            int? userId,
            int? academyId,
            string? action,
            string? targetType,
            int? targetId,
            string? dateFrom,
            string? dateTo,
            int? page,
            int? pageSize)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (academyId.HasValue) query = query.Where(a => a.AcademyId == academyId.Value);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(targetType)) query = query.Where(a => a.TargetType == targetType);
            if (targetId.HasValue) query = query.Where(a => a.TargetId == targetId.Value);

            if (!string.IsNullOrWhiteSpace(dateFrom) || !string.IsNullOrWhiteSpace(dateTo))
            {
                var from = !string.IsNullOrWhiteSpace(dateFrom)
                    ? DateHelper.ParseStartDate(dateFrom)
                    : DateTime.MinValue;
                var to = !string.IsNullOrWhiteSpace(dateTo)
                    ? DateHelper.ParseEndDate(dateTo)
                    : DateTime.UtcNow;

                query = query.Where(a => a.CreatedAt >= from && a.CreatedAt <= to);
            }

            return await PaginateAsync(query, page, pageSize);
        }

        /// <summary>
        /// Owner query — scoped to the caller's academy.
        /// </summary>
        public async Task<object> GetForAcademyAsync(
            int academyId,
            int? userId,
            string? action,
            int? page,
            int? pageSize)
        {
            var query = _db.AuditLogs
                .Where(a => a.AcademyId == academyId);

            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);

            return await PaginateAsync(query, page, pageSize);
        }

        /// <summary>
        /// User's own history — "what have I done recently?"
        /// </summary>
        public async Task<object> GetForUserAsync(
            int userId,
            int? page,
            int? pageSize)
        {
            var query = _db.AuditLogs
                .Where(a => a.UserId == userId);

            return await PaginateAsync(query, page, pageSize);
        }

        /// <summary>
        /// Single log entry with ownership check.
        /// </summary>
        public async Task<object> GetOneAsync(
            int auditId, UserRole role, int? requesterAcademyId)
        {
            var log = await _db.AuditLogs
                .Include(a => a.User)
                .Include(a => a.Academy)
                .FirstOrDefaultAsync(a => a.AuditId == auditId);

            if (log == null)
                throw new InvalidOperationException("Audit log entry not found");

            // Owner scoping — must belong to their academy
            if (role != UserRole.SUPER_ADMIN && log.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("You do not have permission to view this log entry");

            return new
            {
                auditId = log.AuditId,
                userId = log.UserId,
                userName = log.User.FullName,
                userEmail = log.User.Email,
                academyId = log.AcademyId,
                academyName = log.Academy?.Name,
                action = log.Action,
                targetType = log.TargetType,
                targetId = log.TargetId,
                metadata = log.Metadata,
                ipAddress = log.IpAddress,
                createdAt = log.CreatedAt
            };
        }

        // ─── HELPERS ────────────────────────────────────────

        private async Task<object> PaginateAsync(
            IQueryable<AuditLog> query, int? page, int? pageSize)
        {
            var pageNumber = (page.HasValue && page.Value > 0) ? page.Value : 1;
            var size = (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : 20;

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .Include(a => a.User)
                .Include(a => a.Academy)
                .Select(a => new
                {
                    auditId = a.AuditId,
                    userId = a.UserId,
                    userName = a.User.FullName,
                    userEmail = a.User.Email,
                    academyId = a.AcademyId,
                    academyName = a.Academy != null ? a.Academy.Name : null,
                    action = a.Action,
                    targetType = a.TargetType,
                    targetId = a.TargetId,
                    metadata = a.Metadata,
                    ipAddress = a.IpAddress,
                    createdAt = a.CreatedAt
                })
                .ToListAsync();

            return new
            {
                items,
                pagination = new
                {
                    totalRecords = total,
                    pageNumber,
                    pageSize = size,
                    totalPages = (int)Math.Ceiling((double)total / size)
                }
            };
            
        }
    }
}