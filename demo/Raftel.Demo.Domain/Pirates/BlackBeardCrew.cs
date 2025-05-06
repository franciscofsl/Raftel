using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.ValueObjects;

namespace Raftel.Demo.Domain.Pirates;

public static class BlackBeardCrew
{
    public static Pirate Teach() => Pirate.Special(new Name("Marshall D. Teach"), new Bounty(2247600000));

    public static IEnumerable<Pirate> All => new[]
    {
        Teach()
    };
}