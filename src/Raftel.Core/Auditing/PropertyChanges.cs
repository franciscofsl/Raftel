using System.Collections;

namespace Raftel.Core.Auditing;

public class PropertyChanges : IEnumerable<PropertyChange>
{
    public static PropertyChanges Empty => new(Enumerable.Empty<PropertyChange>().ToList());

    private readonly List<PropertyChange> _changes = [];

    [ExcludeFromCodeCoverage]
    private PropertyChanges()
    {
        /* For ORM */
    }

    public PropertyChanges(List<PropertyChange> changes)
    {
        _changes = changes;
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