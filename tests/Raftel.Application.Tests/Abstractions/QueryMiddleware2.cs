using Raftel.Application.Abstractions;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Abstractions;

public class QueryMiddleware2<TRequest, TResponse>(ISpy spy)
    : IQueryMiddleware<TRequest, TResponse> where TRequest : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TRequest request, RequestHandlerDelegate<Result<TResponse>> next)
    {
        spy.Intercept("Hi Query 2");
        var result = await next();
        spy.Intercept("By Query 2");
        return result;
    }
}