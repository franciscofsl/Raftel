using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents the unique identifier for an audit entry.
/// </summary>
public sealed record AuditEntryId : TypedId<Guid>
{
    public AuditEntryId(Guid value) : base(value) { }

    public static AuditEntryId New() => new(Guid.NewGuid());
    public static AuditEntryId Empty() => new(Guid.Empty);
    
    public static implicit operator AuditEntryId(Guid value) => new(value);
    public static implicit operator Guid(AuditEntryId id) => id.Value;
}