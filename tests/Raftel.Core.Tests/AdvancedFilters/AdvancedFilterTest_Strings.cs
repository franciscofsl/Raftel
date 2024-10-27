using Raftel.Core.AdvancedFilters;

namespace Raftel.Core.Tests.AdvancedFilters;

public partial class AdvancedFilterTest
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
    public void AdvancedFilter_ShouldFilterForTextNotStartsWith_IfNameNotStartsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotStartsWith(_ => _.Name, "Lu")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotStartsWith(_ => _.Name, "Zo")
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
    public void AdvancedFilter_ShouldNotFilterForTextEndsWith_IfNameNotEndsWith()
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

    [Fact]
    public void AdvancedFilter_ShouldFilterForTextNotEndsWith_IfNameEndsWith()
    {
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEndsWith(_ => _.Name, "y")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEndsWith(_ => _.Name, "ro")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Contains(_ => _.Name, "ff")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Contains(_ => _.Name, "ro")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotContains(_ => _.Name, "ro")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotContains(_ => _.Name, "u")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Equal(_ => _.Name, "Luffy")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Equal(_ => _.Name, "Zoro")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEqual(_ => _.Name, "Zoro")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEqual(_ => _.Name, "Luffy")
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .In(_ => _.Name, new[] { "Luffy", "Zoro" })
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .In(_ => _.Name, new[] { "Luffy", "Zoro" })
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotIn(_ => _.Name, new[] { "Luffy", "Zoro" })
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotIn(_ => _.Name, new[] { "Luffy", "Zoro" })
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Empty(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Empty(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEmpty(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotEmpty(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Null(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .Null(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotNull(_ => _.Name)
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
        var filter = AdvancedFilter
            .ForModel<Pirate>()
            .NotNull(_ => _.Name)
            .Build();

        var pirate = new Pirate
        {
            Name = null
        };

        filter(pirate).Should().BeFalse();
    }

    private class Pirate
    {
        public string Name { get; set; }
    }
}