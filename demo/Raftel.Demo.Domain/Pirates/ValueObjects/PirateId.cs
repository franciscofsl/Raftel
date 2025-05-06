using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates.ValueObjects;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(NewGuid());
}