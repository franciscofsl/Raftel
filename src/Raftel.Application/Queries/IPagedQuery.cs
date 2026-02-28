namespace Raftel.Application.Queries;

/// <summary>
/// Represents a query that returns a paginated result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of each item in the paged result.</typeparam>
public interface IPagedQuery<TResult> : IQuery<PagedResult<TResult>>
{
    /// <summary>Gets the 1-based page number.</summary>
    int Page { get; }

    /// <summary>Gets the number of items per page.</summary>
    int PageSize { get; }
}
