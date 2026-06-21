using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing.ValueObjects;

public sealed record PropertyChangeId : TypedGuidId
{
    public PropertyChangeId(Guid value) : base(value)
    {
    }

    public static PropertyChangeId New() => new(NewGuid());
}
