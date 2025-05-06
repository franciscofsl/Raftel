using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates.DevilFruits;

public sealed class DevilFruit : Entity<DevilFruitId>
{
    private readonly DevilFruitKind _kind;

    private DevilFruit()
    {
    }

    private DevilFruit(Name name, DevilFruitKind kind)
        : base(DevilFruitId.New())
    {
        Name = name;
        _kind = kind;
    }

    public Name Name { get; }

    public static DevilFruit Logia(Name name)
    {
        return new DevilFruit(name, DevilFruitKind.Logia);
    }

    public static DevilFruit Zoan(Name name)
    {
        return new DevilFruit(name, DevilFruitKind.Zoan);
    }

    public static DevilFruit Paramecia(Name name)
    {
        return new DevilFruit(name, DevilFruitKind.Paramecia);
    }
}