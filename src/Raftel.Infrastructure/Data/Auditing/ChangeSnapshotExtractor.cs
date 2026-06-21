using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Raftel.Domain.Auditing;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Default <see cref="IChangeSnapshotExtractor"/> implementation, based on
/// <see cref="DbContext.ChangeTracker"/>.
/// </summary>
internal sealed class ChangeSnapshotExtractor(AuditOptions auditOptions, IAuditValueSerializer serializer)
    : IChangeSnapshotExtractor
{
    private static readonly HashSet<Type> AuditEntityTypes = [typeof(AuditLog), typeof(EntityChange), typeof(PropertyChange)];

    private static readonly HashSet<string> FrameworkShadowProperties =
    [
        ShadowPropertyNames.IsDeleted,
        ShadowPropertyNames.TenantId,
        ShadowPropertyNames.CreatorId,
        ShadowPropertyNames.CreationTime,
        ShadowPropertyNames.LastModifierId,
        ShadowPropertyNames.LastModificationTime
    ];

    public IReadOnlyList<EntityChangeSnapshot> Extract(DbContext context)
    {
        var snapshots = new List<EntityChangeSnapshot>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!IsAuditable(entry))
            {
                continue;
            }

            var snapshot = ExtractEntityChange(entry);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    private bool IsAuditable(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        var clrType = entry.Metadata.ClrType;
        return !AuditEntityTypes.Contains(clrType) && auditOptions.IsAudited(clrType);
    }

    private EntityChangeSnapshot? ExtractEntityChange(EntityEntry entry)
    {
        var changeType = DetermineChangeType(entry);
        var propertyChanges = ExtractPropertyChanges(entry, changeType);

        if (changeType == AuditChangeType.Updated && propertyChanges.Count == 0)
        {
            return null;
        }

        var entityType = entry.Metadata.ClrType;

        return new EntityChangeSnapshot(
            entityType.FullName ?? entityType.Name,
            changeType,
            ExtractEntityId(entry),
            propertyChanges);
    }

    private static string DetermineChangeType(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditChangeType.Created;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditChangeType.Deleted;
        }

        var isDeletedProperty = entry.Metadata.FindProperty(ShadowPropertyNames.IsDeleted);
        if (isDeletedProperty is not null)
        {
            var isDeletedEntry = entry.Property(ShadowPropertyNames.IsDeleted);
            if (isDeletedEntry.IsModified && Equals(isDeletedEntry.CurrentValue, true))
            {
                return AuditChangeType.Deleted;
            }
        }

        return AuditChangeType.Updated;
    }

    private List<PropertyChangeSnapshot> ExtractPropertyChanges(EntityEntry entry, string changeType)
    {
        var changes = new List<PropertyChangeSnapshot>();

        foreach (var property in entry.Properties)
        {
            if (FrameworkShadowProperties.Contains(property.Metadata.Name))
            {
                continue;
            }

            switch (changeType)
            {
                case AuditChangeType.Created:
                    changes.Add(BuildSnapshot(property, oldValue: null, newValue: property.CurrentValue));
                    break;

                case AuditChangeType.Updated when property.IsModified:
                    changes.Add(BuildSnapshot(property, property.OriginalValue, property.CurrentValue));
                    break;

                case AuditChangeType.Deleted when entry.State == EntityState.Deleted:
                    changes.Add(BuildSnapshot(property, property.OriginalValue, newValue: null));
                    break;
            }
        }

        return changes;
    }

    private PropertyChangeSnapshot BuildSnapshot(PropertyEntry property, object? oldValue, object? newValue)
    {
        var propertyType = property.Metadata.ClrType;

        return new PropertyChangeSnapshot(
            property.Metadata.Name,
            propertyType.FullName ?? propertyType.Name,
            serializer.Serialize(oldValue),
            serializer.Serialize(newValue));
    }

    private string ExtractEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
        {
            return string.Empty;
        }

        var values = primaryKey.Properties
            .Select(p => serializer.Serialize(entry.Property(p.Name).CurrentValue));

        return string.Join(",", values);
    }
}
