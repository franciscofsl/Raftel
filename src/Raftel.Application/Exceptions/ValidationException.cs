using Raftel.Domain.Abstractions;

namespace Raftel.Application.Exceptions;

/// <summary>
/// Exception thrown when a validation process fails.
/// </summary>
public class ValidationException(IEnumerable<Error> errors) : Exception("Validation failed.")
{
    public IReadOnlyList<Error> Errors { get; } = errors.ToList();

    public override string ToString() =>
        $"Validation failed: {string.Join("; ", Errors.Select(e => e.Message))}";
}