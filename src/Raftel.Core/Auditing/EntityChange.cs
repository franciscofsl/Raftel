using Raftel.Core.Attributes;
using Raftel.Core.GuardClauses;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.Auditing;

public class EntityChange
{
    private EntityChange()
    {
        /* For ORM */
    }

    public EntityChange(string entityId, AuditEventKind kind, PropertyChanges properties)
    {
        Id = SequentialGuidGenerator.Create();
        OccurredOn = DateTime.UtcNow;
        EntityId = Ensure.NotNullOrEmpty(entityId, nameof(entityId));
        Kind = Ensure.NotNull(kind, nameof(kind));
        Properties = Ensure.NotNull(properties, nameof(properties));
    }

    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EntityId { get; private set; }
    public AuditEventKind Kind { get; private set; }
    public PropertyChanges Properties { get; private set; }
}