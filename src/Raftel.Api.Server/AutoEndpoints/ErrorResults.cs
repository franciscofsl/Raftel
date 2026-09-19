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
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error is ValidationError validationError)
        {
            extensions["errors"] = FieldErrorsOf(validationError);
        }

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: TitleFor(error.Type),
            extensions: extensions);
    }

    /// <summary>
    /// Groups a <see cref="ValidationError"/>'s aggregated errors by field name, splitting each
    /// <see cref="Error.Code"/> on its first '.' (see the convention documented on
    /// <see cref="Raftel.Domain.Validators.Validator{TModel}"/>). A code without a '.' groups under
    /// an empty field key rather than throwing.
    /// </summary>
    private static Dictionary<string, string[]> FieldErrorsOf(ValidationError validationError) =>
        validationError.Errors
            .GroupBy(FieldNameOf)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

    private static string FieldNameOf(Error error)
    {
        var separatorIndex = error.Code.IndexOf('.');
        return separatorIndex < 0 ? string.Empty : error.Code[..separatorIndex];
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
