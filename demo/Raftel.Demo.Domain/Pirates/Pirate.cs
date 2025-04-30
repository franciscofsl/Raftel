using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private Pirate(PirateId id, Name name, Bounty bounty, bool isKing) : base(id)
    {
        Name = name;
        Bounty = bounty;
        IsKing = isKing;
    }

    private Pirate() : base(new PirateId(Guid.Empty))
    {
    }

    public Name Name { get; private set; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; set; }

    public static Pirate Create(Name name, Bounty bounty, bool isKing = false)
    {
        return new Pirate(PirateId.New(), name, bounty, isKing);
    }
}