using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public class QueryDispatcher(IRequestDispatcher dispatcher) : IQueryDispatcher
{
    public Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query)
        where TQuery : IQuery<TResult>
        => dispatcher.DispatchAsync<TQuery, Result<TResult>>(query);
}