namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents a page of results along with the paging metadata needed to navigate the full set.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public sealed record PagedResult<T> : IPagedResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items belonging to this page.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of matching items across all pages.</param>
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the items belonging to this page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of matching items across all pages.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages, given <see cref="TotalCount"/> and <see cref="PageSize"/>.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether a page before this one exists.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Gets a value indicating whether a page after this one exists.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result for the given page request.
    /// </summary>
    /// <param name="request">The page request the empty result answers.</param>
    public static PagedResult<T> Empty(PageRequest request) =>
        new([], request.Page, request.PageSize, 0);

    /// <summary>
    /// Projects the items of this page to a different shape, preserving paging metadata.
    /// </summary>
    /// <typeparam name="TOut">The type to project items to.</typeparam>
    /// <param name="selector">The projection function.</param>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
