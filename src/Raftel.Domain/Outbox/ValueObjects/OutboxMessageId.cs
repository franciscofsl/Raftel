using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Outbox.ValueObjects;

public sealed record OutboxMessageId : TypedGuidId
{
    public OutboxMessageId(Guid value) : base(value)
    {
    }

    public static OutboxMessageId New() => new(NewGuid());
}
