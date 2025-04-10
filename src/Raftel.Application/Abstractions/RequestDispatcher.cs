using Microsoft.Extensions.DependencyInjection;

namespace Raftel.Application.Abstractions;

public class RequestDispatcher(IServiceProvider serviceProvider) : IRequestDispatcher
{
    public async Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IRequestMiddleware<TRequest, TResponse>>().ToList();

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.HandleAsync(request);

        var pipeline = behaviors
            .Reverse<IRequestMiddleware<TRequest, TResponse>>()
            .Aggregate(handlerDelegate, (next, behavior) => () => behavior.HandleAsync(request, next));

        return await pipeline();
    }
}