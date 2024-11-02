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
}