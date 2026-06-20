using Raftel.Domain.Auditing;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Default <see cref="IAuditLogFactory"/> implementation. Pure mapping from snapshots to the
/// domain model: no EF Core types, no injected services.
/// </summary>
internal sealed class AuditLogFactory : IAuditLogFactory
{
    public AuditLog? Create(
        string command,
        DateTime timestamp,
        Guid? userId,
        string? userName,
        IReadOnlyList<EntityChangeSnapshot> entityChanges)
    {
        if (entityChanges.Count == 0)
        {
            return null;
        }

        var auditLog = AuditLog.For(command, timestamp, userId, userName);

        foreach (var snapshot in entityChanges)
        {
            var entityChange = EntityChange.Create(
                timestamp,
                snapshot.EntityFullName,
                snapshot.ChangeType,
                snapshot.EntityId);

            foreach (var propertyChange in snapshot.PropertyChanges)
            {
                entityChange.RegisterPropertyChange(
                    propertyChange.PropertyName,
                    propertyChange.PropertyType,
                    propertyChange.OldValue,
                    propertyChange.NewValue);
            }

            auditLog.RegisterChange(entityChange);
        }

        return auditLog;
    }
}
