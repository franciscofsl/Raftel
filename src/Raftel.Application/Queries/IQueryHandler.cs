using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query);
}