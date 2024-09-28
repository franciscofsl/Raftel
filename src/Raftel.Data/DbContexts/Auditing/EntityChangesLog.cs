using System.Collections;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityChangesLog(IEnumerable<EntityChange> changes) : IEnumerable<EntityChange>
{
    private readonly IReadOnlyList<EntityChange> _changes = changes.ToList().AsReadOnly();

    public bool IsEmpty() => _changes.Count == 0;

    public IEnumerator<EntityChange> GetEnumerator()
    {
        return _changes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}