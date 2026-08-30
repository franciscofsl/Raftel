using Raftel.Application.Abstractions;
using Raftel.Application.Middlewares;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions;

public class QueryMiddleware1<TRequest, TResponse>(ISpy spy)
    : IQueryMiddleware<TRequest, TResponse> where TRequest : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next, CancellationToken cancellationToken)
    {
        spy.Intercept("Hi Query 1");
        var result = await next(cancellationToken);
        spy.Intercept("By Query 1");
        return result;
    }
}