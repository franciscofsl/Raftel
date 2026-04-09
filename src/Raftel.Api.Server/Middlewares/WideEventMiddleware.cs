using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Middleware that initializes a Wide Event at the start of a request,
/// enriches it with HTTP context data, captures timing and outcome,
/// and emits it as a single structured log entry when the request completes.
/// </summary>
public sealed class WideEventMiddleware(RequestDelegate next, ILogger<WideEventMiddleware> logger)
{
    /// <summary>
    /// Processes the request, building a wide event throughout the lifecycle
    /// and emitting it when the request completes.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var wideEvent = context.RequestServices.GetRequiredService<IWideEvent>();
        var stopwatch = Stopwatch.StartNew();

        InitializeEvent(wideEvent, context);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            CaptureError(wideEvent, ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            CaptureOutcome(wideEvent, context, stopwatch.ElapsedMilliseconds);
            EmitEvent(wideEvent);
        }
    }

    private static void InitializeEvent(IWideEvent wideEvent, HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;

        wideEvent.Add("timestamp", DateTimeOffset.UtcNow);
        wideEvent.Add("request_id", requestId);
        wideEvent.Add("trace_id", traceId);
        wideEvent.Add("method", context.Request.Method);
        wideEvent.Add("path", context.Request.Path.Value ?? string.Empty);
    }

    private static void CaptureError(IWideEvent wideEvent, Exception exception)
    {
        wideEvent.Add("error_type", exception.GetType().Name);
        wideEvent.Add("error_message", exception.Message);
    }

    private static void CaptureOutcome(IWideEvent wideEvent, HttpContext context, long durationMs)
    {
        wideEvent.Add("status_code", context.Response.StatusCode);
        wideEvent.Add("duration_ms", durationMs);

        var outcome = context.Response.StatusCode >= 500 ? "error" : "success";
        wideEvent.Add("outcome", outcome);
    }

    private void EmitEvent(IWideEvent wideEvent)
    {
        var properties = wideEvent.GetProperties();

        using (logger.BeginScope(properties))
        {
            logger.LogInformation(
                "WideEvent {Method} {Path} responded {StatusCode} in {DurationMs}ms",
                properties.GetValueOrDefault("method"),
                properties.GetValueOrDefault("path"),
                properties.GetValueOrDefault("status_code"),
                properties.GetValueOrDefault("duration_ms"));
        }
    }
}
