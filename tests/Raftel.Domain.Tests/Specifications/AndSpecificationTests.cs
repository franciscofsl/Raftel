using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Specifications;
using Shouldly;

namespace Raftel.Domain.Tests.Specifications;

public class AndSpecificationTests
{
    [Fact]
    public void AndSpecification_ShouldSatisfyKingOrBountyCondition()
    {
        var andSpecification = new BountyOverAndKingAndSpecification(50000000);

        andSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        andSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Sanji).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var andSpecification = new BountyOverAndKingAndSpecification(100000000);

        andSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        andSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var andSpecification = new BountyOverAndKingAndSpecification(500000000);

        andSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }
}