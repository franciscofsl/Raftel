using Microsoft.Extensions.DependencyInjection;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Queries;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query);
    }
}