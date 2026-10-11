using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.Concurrency;

/// <summary>
/// Errors reported when a write conflicts with a concurrent change.
/// </summary>
public static class ConcurrencyErrors
{
    /// <summary>
    /// The code of the error returned when a command is rejected as a concurrency conflict.
    /// </summary>
    public const string ConflictCode = "Concurrency.Conflict";

    /// <summary>
    /// Creates the conflict error for the given conflicting entity types.
    /// </summary>
    /// <param name="entityNames">The names of the entity types that were in conflict.</param>
    public static Error Conflict(IReadOnlyCollection<string> entityNames) =>
        Error.Conflict(ConflictCode,
            entityNames.Count == 0
                ? "The data was modified by another request since it was read. Reload it and try again."
                : $"The data ({string.Join(", ", entityNames)}) was modified by another request since it was read. Reload it and try again.");
}
