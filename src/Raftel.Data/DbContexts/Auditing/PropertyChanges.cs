using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Raftel.Data.DbContexts.Auditing;

public class PropertyChanges : IEnumerable<PropertyChange>
{
    public static PropertyChanges Empty => new(Enumerable.Empty<PropertyChange>().ToList());
    
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