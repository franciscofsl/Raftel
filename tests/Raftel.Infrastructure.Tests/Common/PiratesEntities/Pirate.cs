using Raftel.Domain.BaseTypes;
using Raftel.Infrastructure.Tests.Common.PiratesEntities.ValueObjects;

namespace Raftel.Infrastructure.Tests.Common.PiratesEntities;

public class Pirate : AggregateRoot<PirateId>
{
    private Pirate(PirateId id, Name name, Bounty bounty) : base(id)
    {
        Name = name;
        Bounty = bounty;
    }

    private Pirate() : base(new PirateId(Guid.Empty))
    {
    }

    public Name Name { get; private set; }
    public Bounty Bounty { get; private set; }

    public static Pirate Create(Name name, Bounty bounty)
    {
        return new Pirate(PirateId.New(), name, bounty);
    }
}