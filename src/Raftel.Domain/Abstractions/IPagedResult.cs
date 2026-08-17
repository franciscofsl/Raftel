namespace Raftel.Domain.Abstractions;

/// <summary>
/// Non-generic marker for <see cref="PagedResult{T}"/>, letting callers that don't know
/// the item type (e.g. HTTP response mapping) read paging metadata off any paged result.
/// </summary>
public interface IPagedResult
{
    /// <summary>
    /// Gets the total number of matching items across all pages.
    /// </summary>
    long TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    int TotalPages { get; }
}
