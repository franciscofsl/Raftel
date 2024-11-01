using Raftel.Core.AdvancedFilters;
using Raftel.Shared.Common;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterBuilderTest
{
    [Fact]
    public void AdvancedFilter_ShouldFilterForIntEqual_IfBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForIntEqual_IfBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForIntNotEqual_IfBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForIntNotEqual_IfBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForIntIn_IfBountyIsInList()
    {
        var bounties = new int[]
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForIntIn_IfBountyIsNotInList()
    {
        var bounties = new List<int>
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForIntNotIn_IfBountyIsInList_UsingIntArray()
    {
        var bounties = new int[]
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForIntNotIn_IfBountyIsInList_UsingIntList()
    {
        var bounties = new List<int>
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForIntNotIn_IfBountyIsNotInList_UsingIntList()
    {
        var bounties = new List<int>
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForIntNotIn_IfBountyIsNotInList_In_IntArray()
    {
        var bounties = new int[]
        {
            Pirates.Mugiwaras.Luffy.Bounty,
            Pirates.Mugiwaras.Zoro.Bounty,
            Pirates.Mugiwaras.Nami.Bounty
        };

        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, bounties))
            .Build();

        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForNullAge_IfAgeIsNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Age))
            .Build();

        var pirate = new Pirate()
        {
            Age = null
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForNullAge_IfAgeIsNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Age))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForNotNullAge_IfAgeIsNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Age))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForNotNullAge_IfAgeIsNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Age))
            .Build();

        var pirate = new Pirate()
        {
            Age = null
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyGreaterThanOrEqual_IfBountyIsGreaterOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThanOrEqual(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeGreaterThanOrEqual_IfAgeIsGreaterOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThanOrEqual(_ => _.Age, 20))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyGreaterThan_IfBountyIsGreater()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 66000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeGreaterThan_IfAgeIsGreater()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Age, 19))
            .Build();

        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeGreaterThan_IfBountyIsGreater()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, Pirates.Mugiwaras.Chopper.Bounty + 1))
            .Build();

        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyLessThanOrEqual_IfBountyIsLessOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeLessThanOrEqual_IfAgeIsLessOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Age, 19))
            .Build();

        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyLessThan_IfBountyIsLess()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThan(_ => _.Bounty, 1500000000))
            .Build();

        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeLessThan_IfAgeIsLess()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThan(_ => _.Age, 21))
            .Build();

        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyBetween_IfBountyIsInRange()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Between(_ => _.Bounty, new Range<int>(66000000, 500000000)))
            .Build();

        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeBetween_IfAgeIsInRange()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Between(_ => _.Age, new Range<int>(19, 30)))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForBountyNotBetween_IfBountyIsOutsideRange()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotBetween(_ => _.Bounty, new Range<int>(66000000, 500000000)))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeNotBetween_IfAgeIsOutsideRange()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotBetween(_ => _.Age, new Range<int>(19, 30)))
            .Build();

        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
    }
}