using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Middleware that resolves the request's correlation id via <see cref="ICorrelationContext"/>,
/// echoes it back on the response, and owns the current request's <see cref="IRequestEvent"/>
/// lifecycle: it is the single point that emits the request's wide event, exactly once, after
/// every other middleware (including <see cref="ExceptionHandlingMiddleware"/>) has run — so it
/// covers every outcome, including requests that never reach command/query dispatch.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext,
        IRequestEvent requestEvent, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = correlationContext.CorrelationId;
        context.Response.Headers[HeaderName] = correlationId;

        requestEvent.Set("request_id", correlationId);
        requestEvent.Set("method", context.Request.Method);
        requestEvent.Set("path", context.Request.Path.ToString());

        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            requestEvent.Set("duration_ms", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
            requestEvent.Set("status_code", context.Response.StatusCode);

            Emit(logger, requestEvent);
        }
    }

    private static void Emit(ILogger logger, IRequestEvent requestEvent)
    {
        var level = requestEvent.Fields.TryGetValue(RequestEventFields.Level, out var levelValue)
            ? (LogLevel)levelValue
            : LogLevel.Information;

        var exception = requestEvent.Fields.TryGetValue(RequestEventFields.Exception, out var exceptionValue)
            ? (Exception)exceptionValue
            : null;

        logger.Log(level, default, requestEvent.Fields, exception, FormatFields);
    }

    private static string FormatFields(IReadOnlyDictionary<string, object> fields, Exception? exception) =>
        string.Join(", ", fields.Select(field => $"{field.Key}={field.Value}"));
}
