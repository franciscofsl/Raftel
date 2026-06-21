using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing.ValueObjects;

public sealed record AuditLogId : TypedGuidId
{
    public AuditLogId(Guid value) : base(value)
    {
    }

    public static AuditLogId New() => new(NewGuid());
}
