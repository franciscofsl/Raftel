using Microsoft.Extensions.DependencyInjection;

namespace Raftel.Application.Abstractions;

/// <summary>
/// Default implementation of <see cref="IRequestDispatcher"/> that resolves
/// the request handler and middleware pipeline from the dependency injection container.
/// </summary>
public class RequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new instance of the <see cref="RequestDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection service provider.</param>
    public RequestDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = _serviceProvider.GetServices<IRequestMiddleware<TRequest, TResponse>>().ToList();

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.HandleAsync(request);

        var pipeline = behaviors
            .Reverse<IRequestMiddleware<TRequest, TResponse>>()
            .Aggregate(handlerDelegate,
                (next, behavior) => () => behavior.HandleAsync(request, next));

        return await pipeline();
    }
}