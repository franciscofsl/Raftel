using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Raftel.Core.Attributes;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityChange
{
    [ExcludeFromCodeCoverage]
    private EntityChange()
    {
        /* For ORM */
    }

    public DateTime OccurredOn { get; private set; }
    public string EntityId { get; private set; }
    public AuditEventKind Kind { get; private set; }
    public PropertyChanges Properties { get; private set; }

    public static EntityChange Create(EntityEntry entry)
    {
        entry.CurrentValues.TryGetValue<object>("Id", out var idValue);

        return new EntityChange()
        {
            OccurredOn = DateTime.UtcNow,
            EntityId = idValue?.ToString(),
            Kind = entry.State.ToKind(),
            Properties = PropertyChanges.Create(entry)
        };
    }
}

public class PropertyChanges : IEnumerable<PropertyChange>
{
    private readonly List<PropertyChange> _changes = [];

    [ExcludeFromCodeCoverage]
    private PropertyChanges()
    {
        /* For ORM */
    }

    private PropertyChanges(List<PropertyChange> changes)
    {
        _changes = changes;
    }

    public static PropertyChanges Create(EntityEntry entry)
    {
        var propertyChanges = entry.Properties
            .Where(_ => _.Metadata.Name != "Id")
            .WhereIf(entry.State == EntityState.Modified, _ => _.IsModified)
            .Select(PropertyChange.Create).ToList();
        return new PropertyChanges(propertyChanges);
    }

    public IEnumerator<PropertyChange> GetEnumerator()
    {
        return _changes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class PropertyChange
{
    [ExcludeFromCodeCoverage]
    private PropertyChange()
    {
        /* For ORM */
    }

    public static PropertyChange Create(PropertyEntry propertyEntry)
    {
        var oldValue = propertyEntry.EntityEntry.State == EntityState.Added
            ? null
            : propertyEntry.OriginalValue?.ToString();
        return new PropertyChange()
        {
            NewValue = propertyEntry.CurrentValue?.ToString(),
            OldValue = oldValue,
            TypeName = propertyEntry.Metadata.ClrType.FullName,
            Name = propertyEntry.Metadata.Name
        };
    }

    public string Name { get; private set; }
    public string TypeName { get; private set; }
    public string OldValue { get; private set; }
    public string NewValue { get; private set; }
}