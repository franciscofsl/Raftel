namespace Raftel.Application.Abstractions;

/// <summary>
/// Well-known field names written to <see cref="IRequestEvent"/> by framework middleware, so
/// every component agrees on the same keys. Feature code enriching the event with domain fields
/// should follow the same convention with its own <c>&lt;Feature&gt;EventFields</c> constants
/// class rather than inline string literals.
/// </summary>
public static class RequestEventFields
{
    /// <summary>The command/query type name handled during the request.</summary>
    public const string RequestName = "request_name";

    /// <summary>
    /// The <see cref="Microsoft.Extensions.Logging.LogLevel"/> at which the request's wide event
    /// should be emitted. Defaults to <see cref="Microsoft.Extensions.Logging.LogLevel.Information"/>
    /// when not set.
    /// </summary>
    public const string Level = "level";

    /// <summary>The error code of a failed <see cref="Raftel.Domain.Abstractions.Result"/>.</summary>
    public const string ErrorCode = "error.code";

    /// <summary>The error message of a failed <see cref="Raftel.Domain.Abstractions.Result"/>.</summary>
    public const string ErrorMessage = "error.message";

    /// <summary>The exception that terminated the request, if any.</summary>
    public const string Exception = "exception";
}
