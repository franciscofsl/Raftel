using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterBuilderTest
{
    private readonly List<Pirate> _pirates = new()
    {
        new Pirate { Name = "Luffy", LastName = "Monkey D." },
        new Pirate { Name = "Zoro", LastName = "Roronoa" },
        new Pirate { Name = "Nami", LastName = "Swan" },
        new Pirate { Name = "Sanji", LastName = "Vinsmoke" },
        new Pirate { Name = "Tony", LastName = "Tony Chopper" },
        new Pirate { Name = "Robin", LastName = "Nico" },
        new Pirate { Name = "Franky" },
        new Pirate { Name = "Brook" },
        new Pirate { Name = "Jinbe" }
    };

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

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotStartsWith_IfNameNotStartsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "Lu"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotStartsWith_IfNameStartsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "Zo"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEndsWith_IfNameEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "y"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEndsWith_IfNameNotEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "ro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEndsWith_IfNameEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "y"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotEndsWith_IfNameNotEndsWith()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "ro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextContains_IfNameContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "ff"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextContains_IfNameNotContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "ro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotContains_IfNameNotContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotContains(_ => _.Name, "ro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotContains_IfNameContains()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotContains(_ => _.Name, "u"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextEqual_IfNameEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Luffy"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextEqual_IfNameNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Zoro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEqual_IfNameNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Name, "Zoro"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotEqual_IfNameEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Name, "Luffy"))
            .Build();

        var pirate = new Pirate()
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextIn_IfNameInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextIn_IfNameNotInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        var pirate = new Pirate
        {
            Name = "Sanji"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotIn_IfNameNotInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        var pirate = new Pirate
        {
            Name = "Sanji"
        };

        filter(pirate).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldNotFilterForTextNotIn_IfNameInList()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
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

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEmpty_IfNameIsNotEmpty()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEmpty(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
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

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForNotNull_IfPropertyIsNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Name))
            .Build();

        var pirate = new Pirate
        {
            Name = "Luffy"
        };

        filter(pirate).Should().BeTrue();
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

        var chopper = new Pirate { Name = "Chopper" };
        var sanji = new Pirate { Name = "Sanji", LastName = "Vinsmoke" };
        var luffy = new Pirate { Name = "Luffy" };
        var zoro = new Pirate { Name = "Zoro" };

        filter(chopper).Should().BeFalse();
        filter(sanji).Should().BeTrue();
        filter(luffy).Should().BeTrue();
        filter(zoro).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterPiratesByLastName()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(_ => _.Equal(p => p.Name, "Luffy"))
            .Or(p => p.StartsWith(p2 => p2.LastName, "N"))
            .Build();

        var pirates = _pirates.Where(filter).ToList();

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

        var pirates = _pirates.Where(filter).ToList();

        pirates.Should().Contain(_ => _.Name == "Nami");
        pirates.Should().Contain(_ => _.Name == "Sanji");
        pirates.Should().Contain(_ => _.Name == "Franky");
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForEmptyAndNotEmptyLastName()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(builder => builder.NotEmpty(_ => _.Name))
            .Or(p => p.NotEmpty(p => p.Name))
            .Build();

        var emptyLastNamePirate = new Pirate { Name = "Usopp", LastName = "" };
        var nonEmptyNamePirate = new Pirate { Name = "Nami", LastName = "Swan" };
        var emptyNamePirate = new Pirate { Name = "", LastName = "" };

        filter(emptyLastNamePirate).Should().BeTrue();
        filter(nonEmptyNamePirate).Should().BeTrue();
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

        var pirates = _pirates.Where(filter).ToList();

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

        var pirates = _pirates.Where(filter).ToList();

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

        var pirates = _pirates.Where(filter).ToList();

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

        var pirates = _pirates.Where(filter).ToList();

        pirates.Should().HaveCount(3);
    }

    private class Pirate
    {
        public string Name { get; set; }
        public string LastName { get; set; }
    }
}