using Raftel.Domain.Abstractions;

namespace Raftel.Application.Exceptions;

/// <summary>
/// Exception thrown when a validation process fails.
/// </summary>
[Obsolete("The framework's own pipeline no longer throws this; ValidationMiddleware returns a failed Result. " +
          "Inspect Result instead of catching this. Will be removed in a future version.")]
public class ValidationException(IEnumerable<Error> errors) : Exception("Validation failed.")
{
    public IReadOnlyList<Error> Errors { get; } = errors.ToList();

    public override string ToString() =>
        $"Validation failed: {string.Join("; ", Errors.Select(e => e.Message))}";
}