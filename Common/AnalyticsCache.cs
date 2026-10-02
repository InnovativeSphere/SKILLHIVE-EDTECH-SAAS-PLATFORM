using Microsoft.Extensions.Caching.Memory;

namespace SkillHive.Common
{
    /// <summary>
    /// Thin wrapper around IMemoryCache for analytics queries.
    /// Keys are namespaced by role + scope, so one owner's cached dashboard
    /// is never served to another.
    ///
    /// Pattern: cache on read with a TTL. No explicit invalidation — the TTL
    /// handles staleness. 5 minutes is acceptable for analytics.
    /// </summary>
    public class AnalyticsCache
    {
        private readonly IMemoryCache _cache;

        public AnalyticsCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Returns the cached value for the given key, or invokes the factory,
        /// caches the result, and returns it.
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(
            string key,
            TimeSpan ttl,
            Func<Task<T>> factory)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
                return cached;

            var value = await factory();

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            _cache.Set(key, value, options);

            return value;
        }

        /// <summary>
        /// Manual invalidation — call this from write operations if you ever
        /// need a dashboard to refresh immediately instead of waiting for the TTL.
        /// Not used yet; provided for completeness.
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// Builds a standard analytics cache key.
        /// Format: "analytics:{scope}:{endpoint}:{extra}"
        /// Examples:
        ///   "analytics:academy:overview:5"
        ///   "analytics:academy:courses:5:2026-09-01:2026-09-30"
        ///   "analytics:platform:overview"
        ///   "analytics:student:overview:12"
        /// </summary>
        public static string BuildKey(string scope, string endpoint, params object[] parts)
        {
            var suffix = parts.Length > 0
                ? ":" + string.Join(":", parts.Select(p => p?.ToString() ?? "null"))
                : string.Empty;

            return $"analytics:{scope}:{endpoint}{suffix}";
        }
    }
}