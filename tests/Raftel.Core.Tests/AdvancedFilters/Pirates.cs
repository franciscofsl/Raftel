using System.Collections;

namespace Raftel.Core.Tests.AdvancedFilters;

public class Pirates : IEnumerable<Pirate>
{
    private readonly List<Pirate> _pirates;

    private Pirates(List<Pirate> pirates)
    {
        _pirates = pirates;
    }

    public IEnumerator<Pirate> GetEnumerator()
    {
        return _pirates.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static class Mugiwaras
    {
        public static Pirate Luffy => new Pirate { Name = "Luffy", LastName = "Monkey D.", Bounty = 1500000000 };
        public static Pirate Zoro => new Pirate { Name = "Zoro", LastName = "Roronoa", Bounty = 320000000 };
        public static Pirate Nami => new Pirate { Name = "Nami", LastName = "Swan", Bounty = 66000000 };
        public static Pirate Usopp => new Pirate { Name = "Usopp", Bounty = 500000000 };
        public static Pirate Sanji => new Pirate { Name = "Sanji", LastName = "Vinsmoke", Bounty = 330000000 };
        public static Pirate Chopper => new Pirate { Name = "Tony", LastName = "Tony Chopper", Bounty = 100 };
        public static Pirate Robin => new Pirate { Name = "Robin", LastName = "Nico", Bounty = 130000000 };
        public static Pirate Franky => new Pirate { Name = "Franky", Bounty = 94000000 };
        public static Pirate Brook => new Pirate { Name = "Brook", Bounty = 83000000 };
        public static Pirate Jinbe => new Pirate { Name = "Jinbe", Bounty = 438000000 };

        public static Pirates All => new Pirates([
            Luffy,
            Zoro,
            Nami,
            Sanji,
            Chopper,
            Robin,
            Franky,
            Brook,
            Jinbe
        ]);
    }
}