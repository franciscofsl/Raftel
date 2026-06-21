using Raftel.Domain.Auditing.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing;

/// <summary>
/// Represents the audit trail of a single entity affected by a create, update or delete operation.
/// </summary>
public sealed class EntityChange : Entity<EntityChangeId>
{
    private readonly List<PropertyChange> _affectedProperties = [];

    private EntityChange(
        EntityChangeId id,
        DateTime timestamp,
        string entityFullName,
        string changeType,
        string entityId) : base(id)
    {
        Timestamp = timestamp;
        EntityFullName = entityFullName;
        ChangeType = changeType;
        EntityId = entityId;
    }

    private EntityChange()
    {
    }

    public DateTime Timestamp { get; private set; }
    public string EntityFullName { get; private set; }
    public string ChangeType { get; private set; }
    public string EntityId { get; private set; }

    public IReadOnlyCollection<PropertyChange> AffectedProperties => _affectedProperties.AsReadOnly();

    public static EntityChange Create(DateTime timestamp, string entityFullName, string changeType, string entityId) =>
        new(EntityChangeId.New(), timestamp, entityFullName, changeType, entityId);

    public void RegisterPropertyChange(string propertyName, string propertyType, string? oldValue, string? newValue)
    {
        _affectedProperties.Add(PropertyChange.Create(propertyName, propertyType, oldValue, newValue));
    }
}
