namespace Raftel.Application.Queries;

/// <summary>
/// Represents a paginated result set containing a subset of items from a larger collection.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PagedResult{T}"/>.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="totalCount">Total number of items across all pages.</param>
    /// <param name="page">The 1-based current page number.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount must be non-negative.");
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be at least 1.");

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Gets the 1-based current page number.</summary>
    public int Page { get; }

    /// <summary>Gets the maximum number of items per page.</summary>
    public int PageSize { get; }
}
