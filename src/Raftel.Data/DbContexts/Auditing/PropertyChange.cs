using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Raftel.Data.DbContexts.Auditing;

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