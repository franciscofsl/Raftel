namespace Raftel.Data.DbContexts.Auditing;

public class EntityChangesLog(IEnumerable<EntityChange> changes)
{
    private readonly IReadOnlyList<EntityChange> _changes = changes.ToList();
    
    public bool IsEmpty() => !_changes.Any();
}