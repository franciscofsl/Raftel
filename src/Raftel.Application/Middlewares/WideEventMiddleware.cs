using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Global middleware that builds a Wide Event throughout the lifecycle of a request
/// and emits it as a single structured log entry when the request completes.
/// Works across any application type (API, desktop, etc.).
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class WideEventMiddleware<TRequest, TResponse>(
    IWideEvent wideEvent,
    ILogger<WideEventMiddleware<TRequest, TResponse>> logger)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Processes the request, building a wide event throughout the lifecycle
    /// and emitting it when the request completes.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <returns>A task containing the result of the request.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var stopwatch = Stopwatch.StartNew();

        InitializeEvent(request);

        try
        {
            var response = await next();

            stopwatch.Stop();
            CaptureOutcome(response, stopwatch.ElapsedMilliseconds);
            EmitEvent();

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            CaptureError(ex, stopwatch.ElapsedMilliseconds);
            EmitEvent();

            throw;
        }
    }

    private void InitializeEvent(TRequest request)
    {
        wideEvent.Add("timestamp", DateTimeOffset.UtcNow);
        wideEvent.Add("request_type", typeof(TRequest).Name);
        wideEvent.Add("trace_id", Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString());
    }

    private void CaptureOutcome(TResponse response, long durationMs)
    {
        wideEvent.Add("duration_ms", durationMs);

        if (response is Result result)
        {
            wideEvent.Add("outcome", result.IsSuccess ? "success" : "failure");

            if (result.IsFailure)
            {
                wideEvent.Add("error_code", result.Error.Code);
                wideEvent.Add("error_message", result.Error.Message);
            }

            return;
        }

        wideEvent.Add("outcome", "success");
    }

    private void CaptureError(Exception exception, long durationMs)
    {
        wideEvent.Add("duration_ms", durationMs);
        wideEvent.Add("outcome", "error");
        wideEvent.Add("error_type", exception.GetType().Name);
        wideEvent.Add("error_message", exception.Message);
    }

    private void EmitEvent()
    {
        var properties = wideEvent.GetProperties();

        using (logger.BeginScope(properties))
        {
            logger.LogInformation(
                "WideEvent {RequestType} completed with {Outcome} in {DurationMs}ms",
                properties.GetValueOrDefault("request_type"),
                properties.GetValueOrDefault("outcome"),
                properties.GetValueOrDefault("duration_ms"));
        }
    }
}
