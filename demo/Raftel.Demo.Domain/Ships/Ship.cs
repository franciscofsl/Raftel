using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Ships;

public class Ship : AggregateRoot<ShipId>
{
    private Ship(ShipId id, Name name) : base(id)
    {
        Name = name;
    }

    private Ship() : base(new ShipId(Guid.Empty))
    {
    }

    public Name Name { get; private set; }

    public static Ship Create(Name name)
    {
        return new Ship(ShipId.New(), name);
    }
}