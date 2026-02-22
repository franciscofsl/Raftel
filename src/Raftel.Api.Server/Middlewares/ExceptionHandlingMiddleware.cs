using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raftel.Application.Exceptions;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Middleware that catches unhandled exceptions and returns RFC 7807 ProblemDetails responses.
/// Stack traces are never exposed in responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await HandleValidationExceptionAsync(context, exception);
        }
        catch (UnauthorizedException exception)
        {
            await HandleUnauthorizedExceptionAsync(context, exception);
        }
        catch (Exception)
        {
            await HandleInternalServerErrorAsync(context);
        }
    }

    private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = exception.Message,
            Extensions = { ["errors"] = exception.Errors }
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = exception.Message
        };

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(problemDetails);
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
