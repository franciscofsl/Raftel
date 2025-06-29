using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents the unique identifier for an audit property change.
/// </summary>
public sealed record AuditPropertyChangeId : TypedId<Guid>
{
    public AuditPropertyChangeId(Guid value) : base(value) { }

    public static AuditPropertyChangeId New() => new(Guid.NewGuid());
    public static AuditPropertyChangeId Empty() => new(Guid.Empty);
    
    public static implicit operator AuditPropertyChangeId(Guid value) => new(value);
    public static implicit operator Guid(AuditPropertyChangeId id) => id;
}