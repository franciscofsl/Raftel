using Microsoft.AspNetCore.Http;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.Server.AutoEndpoints;

/// <summary>
/// Translates a domain <see cref="Error"/> into an RFC 7807 Problem Details HTTP response.
/// </summary>
public static class ErrorResults
{
    public static IResult ToProblem(Error error)
    {
        var statusCode = StatusCodeFor(error.Type);

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: TitleFor(error.Type),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest
    };

    public static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation Error",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Unexpected => "Internal Server Error",
        _ => "Bad Request"
    };
}
