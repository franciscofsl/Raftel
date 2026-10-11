namespace Raftel.Application.Exceptions;

/// <summary>
/// The exception raised when a save is rejected because another writer changed the same
/// version-tracked entity since it was read.
/// </summary>
/// <remarks>
/// Raised by the persistence layer in place of its own concurrency exception so that no
/// persistence-framework type leaks into the application layer. The command pipeline's
/// concurrency middleware converts it into a failed result; handlers never need to catch it.
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="entityNames">The names of the entity types that were in conflict.</param>
    /// <param name="innerException">The persistence-layer exception that signalled the conflict.</param>
    public ConcurrencyConflictException(IReadOnlyCollection<string> entityNames, Exception innerException = null)
        : base(BuildMessage(entityNames), innerException)
    {
        EntityNames = entityNames;
    }

    /// <summary>
    /// Gets the names of the entity types that were in conflict.
    /// </summary>
    public IReadOnlyCollection<string> EntityNames { get; }

    private static string BuildMessage(IReadOnlyCollection<string> entityNames) =>
        entityNames.Count == 0
            ? "The data was modified by another writer since it was read."
            : $"The data was modified by another writer since it was read: {string.Join(", ", entityNames)}.";
}
