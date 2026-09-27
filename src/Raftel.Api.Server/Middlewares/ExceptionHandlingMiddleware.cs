using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Middleware that catches unhandled exceptions and returns RFC 7807 ProblemDetails responses.
/// Stack traces are never exposed in responses. Enriches the current request's
/// <see cref="IRequestEvent"/> with the exception instead of logging directly — a single
/// component further out in the pipeline emits the request's wide event exactly once.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRequestEvent requestEvent)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            requestEvent.Set(RequestEventFields.Level, LogLevel.Error);
            requestEvent.Set(RequestEventFields.Exception, exception);
            await HandleInternalServerErrorAsync(context);
        }
    }

    private static Task HandleInternalServerErrorAsync(HttpContext context)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred."
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
