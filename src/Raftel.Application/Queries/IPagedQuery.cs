using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

/// <summary>
/// Represents a query that returns a page of <typeparamref name="TItem"/>, accepting the
/// paging and sorting inputs bound from the incoming request's route/query string.
/// </summary>
/// <typeparam name="TItem">The type of the items in the page.</typeparam>
public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    /// <summary>
    /// Gets the requested 1-based page number, or null to use the configured default.
    /// </summary>
    int? Page { get; }

    /// <summary>
    /// Gets the requested page size, or null to use the configured default.
    /// </summary>
    int? PageSize { get; }

    /// <summary>
    /// Gets the requested sort string (e.g. "name,-createdAt"), or null for no client-requested ordering.
    /// </summary>
    string Sort { get; }
}
