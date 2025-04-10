using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public interface IQueryDispatcher
{
    Task<Result<TResponse>> DispatchAsync<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>;
}