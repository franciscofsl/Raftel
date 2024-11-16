using Raftel.Core.AdvancedFilters;
using Raftel.Shared.Common;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterTest
{
    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringEqualAndBountyGreaterThanOrEqual_IfNameEqualAndBountyGreaterThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Luffy"))
            .And(b => b.GreaterThanOrEqual(_ => _.Bounty, 66000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringEqualOrBountyGreaterThanOrEqual_IfNameEqualOrBountyGreaterThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.Equal(_ => _.Name, "Luffy"))
            .Or(b => b.GreaterThanOrEqual(_ => _.Bounty, 66000001))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualAndBountyEqual_IfNameNotEqualAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty).NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualOrBountyEqual_IfNameNotEqualOrBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty)
                .NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringInAndBountyNotEqual_IfNameInAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 66000000).In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringInOrBountyNotEqual_IfNameInOrBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.NotEqual(_ => _.Bounty, 66000000).In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInAndBountyGreaterThan_IfNameNotInAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 101).NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInOrBountyGreaterThan_IfNameNotInOrBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.GreaterThan(_ => _.Bounty, 101).NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringContainsAndBountyLessThanOrEqual_IfNameContainsAndBountyLessThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Bounty, 100000000).Contains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringContainsOrBountyLessThanOrEqual_IfNameContainsOrBountyLessThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .LessThanOrEqual(_ => _.Bounty, 100000000)
                .Contains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsAndBountyLessThan_IfNameNotContainsAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThan(_ => _.Bounty, 500000000).NotContains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsOrBountyLessThan_IfNameNotContainsOrBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .LessThan(_ => _.Bounty, 500000000)
                .NotContains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithAndBountyBetween_IfNameStartsWithAndBountyBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b
                .Between(_ => _.Bounty, new Range<int>(100000000, 500000000))
                .StartsWith(_ => _.Name, "S"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithOrBountyBetween_IfNameStartsWithOrBountyBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .Between(_ => _.Bounty, new Range<int>(100000000, 500000000))
                .StartsWith(_ => _.Name, "S"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithAndBountyNotBetween_IfNameEndsWithAndBountyNotBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotBetween(_ => _.Bounty, new Range<int>(100000000, 500000000)).EndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithOrBountyNotBetween_IfNameEndsWithOrBountyNotBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.NotBetween(_ => _.Bounty, new Range<int>(100000000, 500000000)))
            .Or(b => b.EndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEmptyAndAgeNull_IfNameEmptyAndBountyNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Empty(_ => _.Name))
            .And(b => b.Null(_ => _.Age))
            .Build();

        var pirate = new Pirate()
        {
            Name = string.Empty
        };
        filter(pirate).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEmptyAndAgeNotNull_IfNameNotEmptyAndAgeNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEmpty(_ => _.Name))
            .And(b => b.NotNull(_ => _.Age))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNullAndBountyIn_IfNameNullAndBountyIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Name))
            .And(b => b.In(_ => _.Bounty, new[] { 100000000, 200000000, 300000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotNullAndBountyNotIn_IfNameNotNullAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Name))
            .And(b => b.NotIn(_ => _.Bounty, new[] { 100000000, 200000000, 300000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithAndBountyEqual_IfNameStartsWithAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.StartsWith(_ => _.Name, "L"))
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotStartsWithAndBountyGreaterThanOrEqual_IfNameNotStartsWithAndBountyGreaterThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "L"))
            .And(b => b.GreaterThanOrEqual(_ => _.Bounty, 300000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithAndBountyLessThan_IfNameEndsWithAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "o"))
            .And(b => b.LessThan(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty + 1))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotEndsWithAndBountyGreaterThan_IfNameNotEndsWithAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "y"))
            .And(b => b.GreaterThan(_ => _.Bounty, 500000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringContainsAndBountyNotEqual_IfNameContainsAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "o"))
            .And(b => b.NotEqual(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }
}