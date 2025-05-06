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

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Sanji()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void OrSpecification_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var orSpecification = new BountyOverAndKingOrSpecification(100000000);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void OrSpecification_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var orSpecification = new BountyOverAndKingOrSpecification(500000000);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }
}