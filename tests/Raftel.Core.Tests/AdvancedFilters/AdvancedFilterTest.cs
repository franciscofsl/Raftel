using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public class AdvancedFilterTest
{
    [Fact]
    public void AdvancedFilter_ShouldCreateValidExpression_For_Text_Equals()
    {
        var advancedFilter = AdvancedFilter
            .ForModel<Pirate>()
            .StartsWith(_ => _.Name, "Luffy");

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        advancedFilter.Satisfy(pirate).Should().BeTrue();
    }

    private class Pirate
    {
        public string Name { get; set; }
    }
}