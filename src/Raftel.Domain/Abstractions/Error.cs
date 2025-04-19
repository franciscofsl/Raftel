namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents an error with a specific code and message.
/// </summary>
/// <param name="Code">The unique code identifying the error.</param>
/// <param name="Message">The descriptive message of the error.</param>
public record Error(string Code, string Message)
{
    /// <summary>
    /// Represents the absence of an error.
    /// </summary>
    public static Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error indicating that a null value was provided.
    /// </summary>
    public static Error NullValue = new("Error.NullValue", "Null value was provided");
}