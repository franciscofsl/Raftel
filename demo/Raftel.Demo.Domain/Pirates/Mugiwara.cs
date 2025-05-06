using Raftel.Demo.Domain.Pirates.ValueObjects;

namespace Raftel.Demo.Domain.Pirates;

public static class Mugiwara
{
    public static readonly Pirate Luffy = Pirate.Normal(new Name("Monkey D. Luffy"), new Bounty(300000000));
    public static readonly Pirate Zoro = Pirate.Normal(new Name("Roronoa Zoro"), new Bounty(120000000));
    public static readonly Pirate Nami = Pirate.Normal(new Name("Nami"), new Bounty(16000000));
    public static readonly Pirate Usopp = Pirate.Normal(new Name("Usopp"), new Bounty(30000000));
    public static readonly Pirate Sanji = Pirate.Normal(new Name("Vinsmoke Sanji"), new Bounty(77000000));
    public static readonly Pirate Chopper = Pirate.Normal(new Name("Tony Tony Chopper"), new Bounty(100));
    public static readonly Pirate Robin = Pirate.Normal(new Name("Nico Robin"), new Bounty(79000000));
    public static readonly Pirate Franky = Pirate.Normal(new Name("Franky"), new Bounty(44000000));
    public static readonly Pirate Brook = Pirate.Normal(new Name("Brook"), new Bounty(83000000));
    public static readonly Pirate Jinbe = Pirate.Normal(new Name("Jinbe"), new Bounty(438000000));

    public static IEnumerable<Pirate> All => new[]
    {
        Luffy, Zoro, Nami, Usopp, Sanji, Chopper, Robin, Franky, Brook, Jinbe
    };
}