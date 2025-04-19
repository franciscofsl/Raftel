using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions;

/// <summary>
/// Default implementation of <see cref="IRequestDispatcher"/> that resolves
/// the request handler and middleware pipeline from the dependency injection container.
/// </summary>
public class RequestDispatcher(IServiceProvider serviceProvider) : IRequestDispatcher
{
    /// <inheritdoc />
    public async Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        var allMiddlewares = new List<IGlobalMiddleware<TRequest, TResponse>>();
        allMiddlewares.AddRange(serviceProvider.GetServices<IGlobalMiddleware<TRequest, TResponse>>());
        allMiddlewares.AddRange(GetCommandMiddlewares<TRequest, TResponse>(request));
        allMiddlewares.AddRange(GetQueriesMiddlewares<TRequest, TResponse>(request));

        var handlerDelegate = new RequestHandlerDelegate<TResponse>(() => handler.HandleAsync(request));

        var pipeline = allMiddlewares
            .Reverse<IGlobalMiddleware<TRequest, TResponse>>()
            .Aggregate(handlerDelegate, (next, middleware) => () => middleware.HandleAsync(request, next));

        return await pipeline();
    }

    private IGlobalMiddleware<TRequest, TResponse>[] GetCommandMiddlewares<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>
    {
        if (request is not ICommand || typeof(TResponse) != typeof(Result))
        {
            return [];
        }

        return serviceProvider
            .GetServices(typeof(ICommandMiddleware<>).MakeGenericType(typeof(TRequest)))
            .Cast<IGlobalMiddleware<TRequest, TResponse>>()
            .ToArray();
    }

    private IGlobalMiddleware<TRequest, TResponse>[] GetQueriesMiddlewares<TRequest, TResponse>(
        TRequest request) where TRequest : IRequest<TResponse>
    {
        var requestType = request.GetType();
        if (!requestType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            return [];
        }

        var queryInterface = requestType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

        if (queryInterface == null)
        {
            return [];
        }

        var responseType = queryInterface.GetGenericArguments()[0];
        var middlewareInterface = typeof(IQueryMiddleware<,>).MakeGenericType(requestType, responseType);

        return serviceProvider
            .GetServices(middlewareInterface)
            .Cast<IGlobalMiddleware<TRequest, TResponse>>()
            .ToArray();
    }
}