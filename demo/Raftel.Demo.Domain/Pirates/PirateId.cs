using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(NewGuid());
}