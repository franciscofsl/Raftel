using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

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

        var allMiddlewares = new List<IGlobalMiddleware<TRequest, TResponse>>();

        allMiddlewares.AddRange(_serviceProvider.GetServices<IGlobalMiddleware<TRequest, TResponse>>());
        allMiddlewares.AddRange(GetCommandMiddlewares<TRequest, TResponse>(request));


        if (request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            var middlewares = GetQueryMiddlewares(_serviceProvider, request.GetType());

            foreach (var middleware in middlewares)
            {
                if (middleware is IGlobalMiddleware<TRequest, TResponse> typed)
                {
                    allMiddlewares.Add(typed);
                }
            }
        }

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

        return _serviceProvider
            .GetServices(typeof(ICommandMiddleware<>).MakeGenericType(typeof(TRequest)))
            .Cast<IGlobalMiddleware<TRequest, TResponse>>()
            .ToArray();
    }
 

    private static IEnumerable<object> GetQueryMiddlewares(IServiceProvider serviceProvider, Type requestType)
    {
        var queryInterface = requestType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

        if (queryInterface == null)
        {
            throw new InvalidOperationException($"El tipo {requestType.Name} no implementa IQuery<T>.");
        }

        var responseType = queryInterface.GetGenericArguments()[0];

        var middlewareInterface = typeof(IQueryMiddleware<,>).MakeGenericType(requestType, responseType);

        var middlewares = serviceProvider.GetServices(middlewareInterface);

        return middlewares;
    }
}