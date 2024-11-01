using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterBuilderTest
{
    // Greater Than or equal - pending implement
    // Greater Than  - pending implement
    // Less Than or equal  - pending implement
    // Less Than    - pending implement
    // Between - pending implement
    // Not Between - pending implement
    // Is Null
    // Not Null

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