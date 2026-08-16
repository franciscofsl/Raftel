namespace Raftel.Domain.Abstractions;

/// <summary>
/// Classifies the semantic category of an <see cref="Error"/>.
/// </summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unexpected
}
