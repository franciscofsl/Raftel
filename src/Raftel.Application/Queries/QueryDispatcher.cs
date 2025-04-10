using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

/// <summary>
/// Default implementation of <see cref="IQueryDispatcher"/> that delegates query execution
/// to the <see cref="IRequestDispatcher"/> infrastructure.
/// </summary>
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IRequestDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryDispatcher"/> class.
    /// </summary>
    /// <param name="dispatcher">The internal dispatcher used to route queries.</param>
    public QueryDispatcher(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query)
        where TQuery : IQuery<TResult>
        => _dispatcher.DispatchAsync<TQuery, Result<TResult>>(query);
}