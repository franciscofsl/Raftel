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
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();
        
        andSpecification.IsSatisfiedBy(luffy).ShouldBeTrue();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Sanji()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldSatisfyWhenOneConditionIsTrue()
    {
        var andSpecification = new BountyOverAndKingAndSpecification(100000000);
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();

        andSpecification.IsSatisfiedBy(luffy).ShouldBeTrue();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldNotSatisfyWhenBothConditionsAreFalse()
    {
        var andSpecification = new BountyOverAndKingAndSpecification(500000000);

        andSpecification.IsSatisfiedBy(MugiwaraCrew.Zoro()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Chopper()).ShouldBeFalse();
        andSpecification.IsSatisfiedBy(MugiwaraCrew.Nami()).ShouldBeFalse();
    }
}