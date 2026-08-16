namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents an error with a specific code and message.
/// </summary>
/// <param name="Code">The unique code identifying the error.</param>
/// <param name="Message">The descriptive message of the error.</param>
/// <param name="Type">The semantic category of the error.</param>
public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>
    /// Represents the absence of an error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error indicating that a null value was provided.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");

    /// <summary>
    /// Creates an error indicating a validation failure.
    /// </summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates an error indicating a requested resource was not found.
    /// </summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates an error indicating a conflict with the current state of a resource.
    /// </summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an error indicating the caller is not authenticated.
    /// </summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates an error indicating the caller is not allowed to perform the operation.
    /// </summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>
    /// Creates an unclassified failure error.
    /// </summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
