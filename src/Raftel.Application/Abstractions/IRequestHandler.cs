namespace Raftel.Application.Abstractions;

/// <summary>
/// Defines a handler that processes a request of type <typeparamref name="TRequest"/>
/// and returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the given request asynchronously.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <returns>A task representing the asynchronous operation, containing the response.</returns>
    Task<TResponse> HandleAsync(TRequest request);
}