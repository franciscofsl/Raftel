using Raftel.Domain.Auditing;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Builds an <see cref="AuditLog"/> aggregate from EF Core-agnostic change snapshots.
/// Has no dependency on EF Core or the dependency injection container, making it fully
/// unit-testable in isolation.
/// </summary>
public interface IAuditLogFactory
{
    /// <summary>
    /// Creates an <see cref="AuditLog"/> for the given command/query and change snapshots,
    /// or <c>null</c> if there are no changes to record.
    /// </summary>
    /// <param name="command">The full name of the command or query that caused the changes.</param>
    /// <param name="timestamp">The UTC timestamp at which the changes were captured.</param>
    /// <param name="userId">The identifier of the user that caused the changes, if any.</param>
    /// <param name="userName">The name of the user that caused the changes, if any.</param>
    /// <param name="entityChanges">The entity change snapshots captured for this save operation.</param>
    AuditLog? Create(
        string command,
        DateTime timestamp,
        Guid? userId,
        string? userName,
        IReadOnlyList<EntityChangeSnapshot> entityChanges);
}
