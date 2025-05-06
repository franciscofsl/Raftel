using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Specifications;
using Shouldly;

namespace Raftel.Domain.Tests.Specifications;

public class OrSpecificationTests
{
    [Fact]
    public void OrSpecification_ShouldSatisfyKingOrBountyCondition()
    {
        var orSpecification = new BountyOverAndKingOrSpecification(50000000);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Sanji()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void OrSpecification_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var orSpecification = new BountyOverAndKingOrSpecification(100000000);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void OrSpecification_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var orSpecification = new BountyOverAndKingOrSpecification(500000000);

        orSpecification.IsSatisfiedBy(Mugiwara.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami()).ShouldBeFalse();
    }
}