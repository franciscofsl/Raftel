using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private BodyType _bodyType;

    private Pirate(Name name, Bounty bounty, BodyType bodyType) : this()
    {
        Name = name;
        Bounty = bounty;
        _bodyType = bodyType;
    }

    private Pirate() : base(PirateId.New())
    {
    }

    public Name Name { get; private set; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; private set; }

    public static Pirate Normal(Name name, Bounty bounty)
    {
        return new(name, bounty, BodyType.Normal);
    }

    public void FoundOnePiece()
    {
        IsKing = true;
    }
}