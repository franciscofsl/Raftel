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
    // In
    // Not In
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
}