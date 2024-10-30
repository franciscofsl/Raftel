using Raftel.Core.AdvancedFilters;

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
}