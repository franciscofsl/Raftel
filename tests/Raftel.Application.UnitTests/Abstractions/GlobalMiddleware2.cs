using Raftel.Application.Abstractions;
using Raftel.Application.Middlewares;

namespace Raftel.Application.UnitTests.Abstractions;

public class GlobalMiddleware2<TRequest, TResponse>(ISpy spy) : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{ 

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        spy.Intercept("Hi Global 2");
        var result = await next();
        spy.Intercept("By Global 2");
        return result;
    }
}