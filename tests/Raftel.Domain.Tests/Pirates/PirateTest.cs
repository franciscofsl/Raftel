using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Domain.Tests.Pirates;

public class PirateTest
{
    [Fact]
    public void PirateEatFruit_ShouldEatFruit()
    {
        var pirate = MugiwaraCrew.Luffy();
        var fruit = KnownDevilFruits.GomuGomu();

        pirate.EatFruit(fruit);

        pirate.EatenDevilFruits.ShouldContain(fruit);
    }

    [Fact]
    public void SpecialPirate_ShouldEatMultipleFruits()
    {
        var teach = BlackBeardCrew.Teach();
        var yami = KnownDevilFruits.YamiYami();
        var gura = KnownDevilFruits.GuraGura();

        teach.EatFruit(yami);
        teach.EatFruit(gura);

        teach.EatenDevilFruits.ShouldContain(yami);
        teach.EatenDevilFruits.ShouldContain(gura);
        teach.EatenDevilFruits.Count.ShouldBe(2);
    }

    [Fact]
    public void PirateEatFruit_ShouldNotEat_WhenEatingSecondFruit_AndIsNormal()
    {
        var pirate = MugiwaraCrew.Nami();
        var fruit1 = KnownDevilFruits.GomuGomu();
        var fruit2 = KnownDevilFruits.MeraMera();

        pirate.EatFruit(fruit1);
        var result = pirate.EatFruit(fruit2);
        result.Error.ShouldBe(PirateErrors.CannotEatMoreThanOneDevilFruit);

        pirate.EatenDevilFruits.Count.ShouldBe(1);
    }
}