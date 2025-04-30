using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Specifications;
using Shouldly;

namespace Raftel.Domain.Tests.Specifications;

public class SpecificationTests
{
    [Fact]
    public void IsKingSpecification_ShouldSatisfyKingCondition()
    {
        var isKingSpec = new IsKingSpecification();

        isKingSpec.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        isKingSpec.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        isKingSpec.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
    }

    [Fact]
    public void IsKingSpecification_ShouldNotSatisfyWhenNotKing()
    {
        var isKingSpec = new IsKingSpecification();

        isKingSpec.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        isKingSpec.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldSatisfyKingOrBountyCondition()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver50M = new BountyOverSpecification(50000000);

        var orSpecification = isKingSpec.Or(bountyOver50M);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Sanji).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver100M = new BountyOverSpecification(100000000);

        var orSpecification = isKingSpec.Or(bountyOver100M);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver500M = new BountyOverSpecification(500000000);

        var orSpecification = isKingSpec.Or(bountyOver500M);

        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldSatisfyKingOrBountyCondition()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver50M = new BountyOverSpecification(50000000);

        var orSpecification = isKingSpec.And(bountyOver50M);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Sanji).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver100M = new BountyOverSpecification(100000000);

        var orSpecification = isKingSpec.And(bountyOver100M);

        orSpecification.IsSatisfiedBy(Mugiwara.Luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver500M = new BountyOverSpecification(500000000);

        var orSpecification = isKingSpec.And(bountyOver500M);

        orSpecification.IsSatisfiedBy(Mugiwara.Zoro).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Chopper).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(Mugiwara.Nami).ShouldBeFalse();
    }
}