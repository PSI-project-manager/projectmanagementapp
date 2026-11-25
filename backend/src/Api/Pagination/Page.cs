using Microsoft.EntityFrameworkCore;

namespace Api.Pagination;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public const int MaxPageSize = 100;

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => PageNumber < TotalPages;

    public Page<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Page<TOut>(Items.Select(selector).ToList(), PageNumber, PageSize, TotalCount);
    }

    public static async Task<Page<T>> FromQueryAsync(
        IQueryable<T> query,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default
    )
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new Page<T>(items, pageNumber, pageSize, total);
    }
}
