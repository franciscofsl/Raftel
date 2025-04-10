namespace Raftel.Application.Abstractions;

/// <summary>
/// Dispatches requests through the pipeline and to their associated handler.
/// </summary>
public interface IRequestDispatcher
{
    /// <summary>
    /// Dispatches the given request and returns the result asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to dispatch.</param>
    /// <returns>The result returned from the request handler or middleware.</returns>
    Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>;
}