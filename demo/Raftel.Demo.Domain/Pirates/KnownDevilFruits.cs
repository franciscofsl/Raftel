using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits;

namespace Raftel.Demo.Domain.Pirates;

public static class KnownDevilFruits
{
    public static DevilFruit GomuGomu() => DevilFruit.Paramecia(new Name("Gomu Gomu"));
    public static DevilFruit HitoHitoNika() => DevilFruit.Zoan(new Name("Hito Hito model Nika"));
    public static DevilFruit BaraBara() => DevilFruit.Paramecia(new Name("Bara Bara"));
    public static DevilFruit MeraMera() => DevilFruit.Logia(new Name("Mera Mera"));
    public static DevilFruit GuraGura() => DevilFruit.Paramecia(new Name("Gura Gura"));
    public static DevilFruit YamiYami() => DevilFruit.Logia(new Name("Yami Yami"));

    public static IEnumerable<DevilFruit> All => new[]
    {
        GomuGomu(), HitoHitoNika(), BaraBara(), MeraMera(), GuraGura(), YamiYami()
    };
}