using Raftel.Core.AdvancedFilters;

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

}