using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Ships;

public sealed record ShipId : TypedGuidId
{
    public ShipId(Guid value) : base(value)
    {
    }

    public static ShipId New() => new(NewGuid());
}