using Microsoft.EntityFrameworkCore;

namespace SkillHive.Common
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    public static class PaginationHelper
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public static (int pageNumber, int pageSize) Normalize(int? page, int? pageSize)
        {
            var pageNumber = (page.HasValue && page.Value > 0) ? page.Value : 1;
            var size = (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : DefaultPageSize;

            if (size > MaxPageSize)
                size = MaxPageSize;

            return (pageNumber, size);
        }

        public static async Task<PaginatedResult<T>> PaginateAsync<T>(
            IQueryable<T> query,
            int? page,
            int? pageSize)
        {
            var (pageNumber, size) = Normalize(page, pageSize);

            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PaginatedResult<T>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = size
            };
        }
    }
}