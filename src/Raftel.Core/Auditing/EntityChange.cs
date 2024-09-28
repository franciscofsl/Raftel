using Raftel.Core.Attributes;
using Raftel.Core.GuardClauses;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.Auditing;

public class EntityChange
{
    private readonly IReadOnlyList<PropertyChange> _properties;

    private EntityChange()
    {
        /* For ORM */
    }

    public EntityChange(string entityId, AuditEventKind kind, IReadOnlyList<PropertyChange> properties)
    {
        Id = SequentialGuidGenerator.Create();
        OccurredOn = DateTime.UtcNow;
        EntityId = Ensure.NotNullOrEmpty(entityId, nameof(entityId));
        Kind = Ensure.NotNull(kind, nameof(kind));
        _properties = Ensure.NotNull(properties, nameof(properties));
    }

    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EntityId { get; private set; }
    public AuditEventKind Kind { get; private set; }
    public IReadOnlyCollection<PropertyChange> Properties => _properties;
}