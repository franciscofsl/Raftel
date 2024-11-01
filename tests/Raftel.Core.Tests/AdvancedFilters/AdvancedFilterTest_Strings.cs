using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterBuilderTest
{
    [Fact]
    public void AdvancedFilter_ShouldFilterForTextStartsWith_IfNameStartsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.StartsWith(_ => _.Name, "Lu"))
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
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.StartsWith(_ => _.Name, "Zo"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotStartsWith_IfNameNotStartsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "Lu"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotStartsWith_IfNameStartsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "Zo"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEndsWith_IfNameEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEndsWith_IfNameNotEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "ro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEndsWith_IfNameEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotEndsWith_IfNameNotEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "ro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextContains_IfNameContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "ff"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextContains_IfNameNotContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "ro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotContains_IfNameNotContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotContains(_ => _.Name, "ro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotContains_IfNameContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotContains(_ => _.Name, "u"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEqual_IfNameEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEqual_IfNameNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Zoro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEqual_IfNameNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Name, "Zoro"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotEqual_IfNameEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextIn_IfNameInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextIn_IfNameNotInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotIn_IfNameNotInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotIn_IfNameInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEmpty_IfNameIsEmpty()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Empty(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = string.Empty
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEmpty_IfNameIsNotEmpty()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Empty(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEmpty_IfNameIsNotEmpty()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEmpty(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotEmpty_IfNameIsEmpty()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEmpty(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = string.Empty
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForNull_IfPropertyIsNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = null
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForNull_IfPropertyIsNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForNotNull_IfPropertyIsNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForNotNull_IfPropertyIsNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = null
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForCombinedConditions()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Sanji"))
            .And(_ => _.StartsWith(p => p.LastName, "V"))
            .Or(_ => _.StartsWith(p => p.Name, "L"))
            .Build();

        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterPiratesByLastName()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(_ => _.Equal(p => p.Name, "Luffy"))
            .Or(p => p.StartsWith(p2 => p2.LastName, "N"))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(2);
        pirates.Should().ContainSingle(_ => _.Name == "Luffy");
        pirates.Should().ContainSingle(_ => _.LastName == "Nico");
    }

    [Fact]
    public void AdvancedFilter_ShouldCombineFiltersCorrectlyWithNames()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(p => p.Name, "Nami"))
            .And(p => p.StartsWith(p2 => p2.LastName, "V"))
            .Or(p => p.NotNull(p2 => p2.LastName))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(6);
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForEmptyAndNotEmptyLastName()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(builder => builder.NotEmpty(_ => _.Name))
            .Or(p => p.NotEmpty(p => p.Name))
            .Build();

        var emptyNamePirate = new Pirate { Name = "", LastName = "" };

        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(emptyNamePirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterWithNested_WithOrConditions()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(builder => builder
                .Equal(_ => _.Name, "Luffy")
                .Equal(_ => _.Name, "Zoro"))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(2);
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterWithNestedOrInsideAndConditions()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(builder => builder
                .Contains(_ => _.LastName, "Monkey D.")
                .Or(orBuilder => orBuilder
                    .Equal(_ => _.Name, "Luffy")
                    .Equal(_ => _.Name, "Zoro")))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(1);
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterWithNestedAndInsideOrConditions()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(builder => builder
                .Equal(_ => _.Name, "Luffy")
                .And(andBuilder => andBuilder
                    .Equal(_ => _.Name, "Zoro")
                    .Equal(_ => _.LastName, "Roronoa")))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(2);
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterWithMultipleNestedOrConditions()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(builder => builder
                .Equal(_ => _.Name, "Luffy")
                .Or(orBuilder => orBuilder
                    .Equal(_ => _.Name, "Zoro")
                    .Equal(_ => _.Name, "Sanji")))
            .Build();

        var pirates = Pirates.Mugiwaras.All.Where(filter).ToList();

        pirates.Should().HaveCount(3);
    }
}