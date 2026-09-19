using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Global middleware that enriches the current request's <see cref="IRequestEvent"/> with the
/// request name and outcome. It never logs directly — a single component further out in the
/// pipeline emits the request's wide event exactly once — and never includes request property
/// values, so credentials or other sensitive request payloads never reach a log sink.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <param name="requestEvent">The current request's wide event.</param>
public class LoggingMiddleware<TRequest, TResponse>(IRequestEvent requestEvent) : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Enriches the request event with the request name, then invokes the next delegate in the
    /// pipeline. On a failed <see cref="Result"/>, adds the error code/message and sets the
    /// event's level to <see cref="LogLevel.Warning"/>. On a thrown exception, adds the exception,
    /// sets the level to <see cref="LogLevel.Error"/>, and rethrows.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The response returned by the next handler.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        requestEvent.Set(RequestEventFields.RequestName, typeof(TRequest).Name);

        try
        {
            var response = await next(cancellationToken);

            if (response is Result { IsFailure: true } failure)
            {
                requestEvent.Set(RequestEventFields.Level, LogLevel.Warning);
                requestEvent.Set(RequestEventFields.ErrorCode, failure.Error.Code);
                requestEvent.Set(RequestEventFields.ErrorMessage, failure.Error.Message);
            }

            return response;
        }
        catch (Exception exception)
        {
            requestEvent.Set(RequestEventFields.Level, LogLevel.Error);
            requestEvent.Set(RequestEventFields.Exception, exception);
            throw;
        }
    }
}
