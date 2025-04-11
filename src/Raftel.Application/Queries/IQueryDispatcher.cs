using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

/// <summary>
/// Dispatches a query to its corresponding handler and returns the result.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatches a query asynchronously and retrieves the result.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of the result expected from the query.</typeparam>
    /// <param name="query">The query instance to process.</param>
    /// <returns>A <see cref="Result{TResult}"/> representing the outcome of the query execution.</returns>
    Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query)
        where TQuery : IQuery<TResult>;
}