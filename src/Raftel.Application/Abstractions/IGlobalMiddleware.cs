namespace Raftel.Application.Abstractions;

/// <summary>
/// Represents a middleware component in the request pipeline that can intercept and
/// modify the processing of a request before and/or after the next component is invoked.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Processes the request and optionally calls the next delegate in the pipeline.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <returns>A task containing the result of the request.</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next);
}