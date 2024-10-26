using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public class AdvancedFilterTest
{
    [Fact]
    public void AdvancedFilter_ShouldFilterForTextStartsWith_IfNameStartsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .StartsWith(_ => _.Name, "Lu")
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextStartsWith_IfNameNotStartsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .StartsWith(_ => _.Name, "Zo")
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEndsWith_IfNameStartsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .EndsWith(_ => _.Name, "y")
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEndsWith_IfNameNotStartsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .EndsWith(_ => _.Name, "ro")
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    private class Pirate
    {
        public string Name { get; set; }
    }
}