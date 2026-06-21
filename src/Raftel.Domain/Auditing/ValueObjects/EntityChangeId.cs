using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing.ValueObjects;

public sealed record EntityChangeId : TypedGuidId
{
    public EntityChangeId(Guid value) : base(value)
    {
    }

    public static EntityChangeId New() => new(NewGuid());
}
