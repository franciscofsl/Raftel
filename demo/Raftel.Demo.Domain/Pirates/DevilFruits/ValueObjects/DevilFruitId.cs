using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates.DevilFruits.ValueObjects;

public sealed record DevilFruitId : TypedGuidId
{
    public DevilFruitId(Guid value) : base(value)
    {
    }

    public static DevilFruitId New() => new(NewGuid());
}