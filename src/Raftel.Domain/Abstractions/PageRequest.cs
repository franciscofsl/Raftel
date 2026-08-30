namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents a validated, 1-based page request with a bounded page size.
/// </summary>
public sealed record PageRequest
{
    /// <summary>
    /// The page size used when none is supplied.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// The largest page size a caller may request.
    /// </summary>
    public const int MaxPageSize = 200;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the number of items to skip to reach this page.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Creates a validated <see cref="PageRequest"/>.
    /// </summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A successful result with the page request, or a validation failure.</returns>
    public static Result<PageRequest> Create(int page = 1, int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            return Result.Failure<PageRequest>(Error.Validation("Page.Invalid", "Page must be greater than zero."));
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            return Result.Failure<PageRequest>(Error.Validation("PageSize.Invalid",
                $"Page size must be between 1 and {MaxPageSize}."));
        }

        return Result.Success(new PageRequest(page, pageSize));
    }
}
