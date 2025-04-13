using Raftel.Domain.Tests.Common.Domain.ValueObjects;

namespace Raftel.Domain.Tests.Common.Domain;

public static class Mugiwara
{
    public static readonly Pirate Luffy = Pirate.Create(new Name("Monkey D. Luffy"), new Bounty(300000000), true);
    public static readonly Pirate Zoro = Pirate.Create(new Name("Roronoa Zoro"), new Bounty(120000000));
    public static readonly Pirate Nami = Pirate.Create(new Name("Nami"), new Bounty(16000000));
    public static readonly Pirate Usopp = Pirate.Create(new Name("Usopp"), new Bounty(30000000));
    public static readonly Pirate Sanji = Pirate.Create(new Name("Vinsmoke Sanji"), new Bounty(77000000));
    public static readonly Pirate Chopper = Pirate.Create(new Name("Tony Tony Chopper"), new Bounty(100));
    public static readonly Pirate Robin = Pirate.Create(new Name("Nico Robin"), new Bounty(79000000));
    public static readonly Pirate Franky = Pirate.Create(new Name("Franky"), new Bounty(44000000));
    public static readonly Pirate Brook = Pirate.Create(new Name("Brook"), new Bounty(83000000));
    public static readonly Pirate Jinbe = Pirate.Create(new Name("Jinbe"), new Bounty(438000000));

    public static IEnumerable<Pirate> All => new[]
    {
        Luffy, Zoro, Nami, Usopp, Sanji, Chopper, Robin, Franky, Brook, Jinbe
    };
}