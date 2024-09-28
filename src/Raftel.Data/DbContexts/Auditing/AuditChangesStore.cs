using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Raftel.Core.Attributes;
using Raftel.Core.Auditing;

namespace Raftel.Data.DbContexts.Auditing;

public class AuditChangesStore
{
    public EntityChangesLog CreateLog(ChangeTracker changeTracker)
    {
        var entries = changeTracker
            .Entries()
            .Where(_ => _.Entity.GetType().GetCustomAttribute(typeof(AuditableAttribute)) != null)
            .Where(_ => _.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(EntityEntryToEntityChange)
            .ToList();

        return new EntityChangesLog(entries);
    }

    private static EntityChange EntityEntryToEntityChange(EntityEntry entry)
    {
        entry.CurrentValues.TryGetValue<object>("Id", out var idValue);
        var properties = EntityEntryPropertiesToPropertyChanges(entry);
        return new EntityChange(idValue?.ToString(), entry.State.ToKind(), properties);
    }

    private static IReadOnlyList<PropertyChange> EntityEntryPropertiesToPropertyChanges(EntityEntry entry)
    {
        if (entry.State is EntityState.Deleted)
        {
            return Enumerable.Empty<PropertyChange>().ToList();
        }

        return entry.Properties
            .Where(_ => _.Metadata.Name != "Id")
            .WhereIf(entry.State == EntityState.Modified, _ => _.IsModified)
            .Select(PropertyEntryToPropertyChange)
            .ToList();
    }

    private static PropertyChange PropertyEntryToPropertyChange(PropertyEntry propertyEntry)
    {
        var oldValue = propertyEntry.EntityEntry.State == EntityState.Added
            ? null
            : propertyEntry.OriginalValue?.ToString();
        var currentValue = propertyEntry.CurrentValue?.ToString();

        return new PropertyChange(propertyEntry.Metadata.Name,
            propertyEntry.Metadata.ClrType.FullName, oldValue,
            currentValue);
    }
}