using Raftel.Domain.Abstractions;

namespace Raftel.Application.Exceptions;

/// <summary>
/// Exception thrown when a validation process fails.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public ValidationException(IEnumerable<Error> errors)
        : base("Validation failed.")
    {
        Errors = errors.ToList();
    }

    public override string ToString() =>
        $"Validation failed: {string.Join("; ", Errors.Select(e => e.Message))}";
}