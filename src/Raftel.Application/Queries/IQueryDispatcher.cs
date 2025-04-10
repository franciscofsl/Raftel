using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public interface IQueryDispatcher
{
    Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}