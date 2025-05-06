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
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();

        isKingSpec.IsSatisfiedBy(luffy).ShouldBeTrue();
        isKingSpec.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        isKingSpec.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void IsKingSpecification_ShouldNotSatisfyWhenNotKing()
    {
        var isKingSpec = new IsKingSpecification();

        isKingSpec.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        isKingSpec.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldSatisfyKingOrBountyCondition()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver50M = new BountyOverSpecification(50000000);

        var orSpecification = isKingSpec.Or(bountyOver50M);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Sanji()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver100M = new BountyOverSpecification(100000000);

        var orSpecification = isKingSpec.Or(bountyOver100M);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Luffy()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_OrCombined_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver500M = new BountyOverSpecification(500000000);

        var orSpecification = isKingSpec.Or(bountyOver500M);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldSatisfyKingOrBountyCondition()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver50M = new BountyOverSpecification(50000000);

        var orSpecification = isKingSpec.And(bountyOver50M);
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();

        orSpecification.IsSatisfiedBy(luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Sanji()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver100M = new BountyOverSpecification(100000000);

        var orSpecification = isKingSpec.And(bountyOver100M);
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();

        orSpecification.IsSatisfiedBy(luffy).ShouldBeTrue();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void Specification_AndCombined_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var isKingSpec = new IsKingSpecification();
        var bountyOver500M = new BountyOverSpecification(500000000);

        var orSpecification = isKingSpec.And(bountyOver500M);

        orSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        orSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }
}