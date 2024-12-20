﻿using Raftel.Core.AdvancedFilters;
using Raftel.Shared.Common;

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

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringEqualOrBountyGreaterThanOrEqual_IfNameEqualOrBountyGreaterThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.Equal(_ => _.Name, "Luffy"))
            .Or(b => b.GreaterThanOrEqual(_ => _.Bounty, 66000001))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualAndBountyEqual_IfNameNotEqualAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty).NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualOrBountyEqual_IfNameNotEqualOrBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty)
                .NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringInAndBountyNotEqual_IfNameInAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 66000000).In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringInOrBountyNotEqual_IfNameInOrBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.NotEqual(_ => _.Bounty, 66000000).In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInAndBountyGreaterThan_IfNameNotInAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 101).NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInOrBountyGreaterThan_IfNameNotInOrBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.GreaterThan(_ => _.Bounty, 101).NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringContainsAndBountyLessThanOrEqual_IfNameContainsAndBountyLessThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Bounty, 100000000).Contains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringContainsOrBountyLessThanOrEqual_IfNameContainsOrBountyLessThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .LessThanOrEqual(_ => _.Bounty, 100000000)
                .Contains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsAndBountyLessThan_IfNameNotContainsAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThan(_ => _.Bounty, 500000000).NotContains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsOrBountyLessThan_IfNameNotContainsOrBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .LessThan(_ => _.Bounty, 500000000)
                .NotContains(_ => _.Name, "Nami"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithAndBountyBetween_IfNameStartsWithAndBountyBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b
                .Between(_ => _.Bounty, new Range<int>(100000000, 500000000))
                .StartsWith(_ => _.Name, "S"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithOrBountyBetween_IfNameStartsWithOrBountyBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .Between(_ => _.Bounty, new Range<int>(100000000, 500000000))
                .StartsWith(_ => _.Name, "S"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithAndBountyNotBetween_IfNameEndsWithAndBountyNotBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotBetween(_ => _.Bounty, new Range<int>(100000000, 500000000)).EndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithOrBountyNotBetween_IfNameEndsWithOrBountyNotBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b.NotBetween(_ => _.Bounty, new Range<int>(100000000, 500000000)))
            .Or(b => b.EndsWith(_ => _.Name, "y"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEmptyAndAgeNull_IfNameEmptyAndBountyNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Empty(_ => _.Name))
            .And(b => b.Null(_ => _.Age))
            .Build();

        var pirate = new Pirate()
        {
            Name = string.Empty
        };
        filter(pirate).Should().BeTrue();
        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEmptyAndAgeNotNull_IfNameNotEmptyAndAgeNotNull()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEmpty(_ => _.Name))
            .And(b => b.NotNull(_ => _.Age))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNullAndBountyIn_IfNameNullAndBountyIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Null(_ => _.Name))
            .And(b => b.In(_ => _.Bounty, new[] { 100000000, 200000000, 300000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotNullAndBountyNotIn_IfNameNotNullAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotNull(_ => _.Name))
            .And(b => b.NotIn(_ => _.Bounty, new[] { 100000000, 200000000, 300000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithAndBountyEqual_IfNameStartsWithAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.StartsWith(_ => _.Name, "L"))
            .And(b => b.Equal(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotStartsWithAndBountyGreaterThanOrEqual_IfNameNotStartsWithAndBountyGreaterThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotStartsWith(_ => _.Name, "L"))
            .And(b => b.GreaterThanOrEqual(_ => _.Bounty, 300000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithAndBountyLessThan_IfNameEndsWithAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.EndsWith(_ => _.Name, "o"))
            .And(b => b.LessThan(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty + 1))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotEndsWithAndBountyGreaterThan_IfNameNotEndsWithAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEndsWith(_ => _.Name, "y"))
            .And(b => b.GreaterThan(_ => _.Bounty, 500000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringContainsAndBountyNotEqual_IfNameContainsAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Contains(_ => _.Name, "o"))
            .And(b => b.NotEqual(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsAndBountyEqual_IfNameNotContainsAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotContains(_ => _.Name, "a"))
            .And(b => b.Equal(_ => _.Bounty, 100))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEqualAndBountyNotIn_IfNameEqualAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Name, "Luffy"))
            .And(b => b.NotIn(_ => _.Bounty, new[] { 1500000000, 320000000, 500000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualAndBountyIn_IfNameNotEqualAndBountyIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Name, "Sanji"))
            .And(b => b.In(_ => _.Bounty, new[] { 66000000, 130000000, 330000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEmptyAndBountyGreaterThan_IfNameEmptyAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 500000000))
            .And(b => b.Empty(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotEmptyAndBountyLessThanOrEqual_IfNameNotEmptyAndBountyLessThanOrEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Bounty, 500000000))
            .And(b => b.NotEmpty(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEqualAndBountyNotIn_IfNameNotEqualAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, new[] { 1500000000, 320000000 }))
            .And(b => b.NotEqual(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringContainsAndBountyEqual_IfNameContainsAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, 330000000))
            .And(b => b.Contains(_ => _.Name, "San"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotContainsAndBountyNotEqual_IfNameNotContainsAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 330000000))
            .And(b => b.NotContains(_ => _.Name, "San"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringStartsWithAndBountyNotIn_IfNameStartsWithAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotIn(_ => _.Bounty, new[] { 1500000000, 320000000 }))
            .And(b => b.StartsWith(_ => _.Name, "L"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEndsWithAndBountyIn_IfNameEndsWithAndBountyIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.In(_ => _.Bounty, new[] { 320000000, 330000000 }))
            .And(b => b.EndsWith(_ => _.Name, "o"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForStringNotStartsWithAndBountyNotEqual_IfNameNotStartsWithAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 330000000))
            .And(b => b.NotStartsWith(_ => _.Name, "L"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringEqualAndBountyGreaterThan_IfNameEqualAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 500000000))
            .And(b => b.Equal(_ => _.Name, "Luffy"))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNullAndBountyEqual_IfNameNullAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, 330000000))
            .And(b => b.Null(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotNullAndBountyNotEqual_IfNameNotNullAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 330000000))
            .And(b => b.NotNull(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInAndBountyBetween_IfNameNotBetweenAndBountyBetween()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Between(_ => _.Bounty, new Range<decimal>(0, 200000000)))
            .And(b => b.NotIn(_ => _.Name, new string[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringInAndBountyEqual_IfNameInAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .Or(b => b
                .Equal(_ => _.Bounty, Pirates.Mugiwaras.Luffy.Bounty)
                .Equal(_ => _.Bounty, Pirates.Mugiwaras.Zoro.Bounty))
            .And(b => b.In(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotInAndBountyNotEqual_IfNameNotInAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Bounty, 320000000))
            .And(b => b.NotIn(_ => _.Name, new[] { "Luffy", "Zoro" }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotEmptyAndBountyEqual_IfNameNotEmptyAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Bounty, 330000000))
            .And(b => b.NotEmpty(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNullAndBountyGreaterThan_IfNameNullAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Bounty, 100000000))
            .And(b => b.Null(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForStringNotNullAndBountyLessThan_IfNameNotNullAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThan(_ => _.Bounty, 5000000))
            .And(b => b.NotNull(_ => _.Name))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeEqualAndBountyEqual_IfAgeEqualAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Age, 21))
            .And(b => b.Equal(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeNotEqualAndBountyNotEqual_IfAgeNotEqualAndBountyNotEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Age, 20))
            .And(b => b.NotEqual(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeGreaterThanAndBountyEqual_IfAgeGreaterThanAndBountyEqual()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThan(_ => _.Age, 20))
            .And(b => b.Equal(_ => _.Bounty, 320000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForAgeLessThanOrEqualAndBountyGreaterThan_IfAgeLessThanOrEqualAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.LessThanOrEqual(_ => _.Age, 18))
            .And(b => b.GreaterThan(_ => _.Bounty, 200000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeBetweenAndBountyLessThan_IfAgeBetweenAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Between(_ => _.Age, new Range<int>(18, 22)))
            .And(b => b.LessThan(_ => _.Bounty, 400000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeTrue();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeNotBetweenAndBountyGreaterThan_IfAgeNotBetweenAndBountyGreaterThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotBetween(_ => _.Age, new Range<int>(20, 30)))
            .And(b => b.GreaterThan(_ => _.Bounty, 300000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeEqualAndBountyIn_IfAgeEqualAndBountyIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.Equal(_ => _.Age, 19))
            .And(b => b.In(_ => _.Bounty, new[] { 320000000, 400000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeFalse();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeFalse();
        filter(Pirates.Mugiwaras.Franky).Should().BeFalse();
        filter(Pirates.Mugiwaras.Brook).Should().BeFalse();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }

    [Fact]
    public void AdvancedFilter_ShouldFilterForAgeNotEqualAndBountyNotIn_IfAgeNotEqualAndBountyNotIn()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.NotEqual(_ => _.Age, 22))
            .And(b => b.NotIn(_ => _.Bounty, new[] { 320000000, 400000000 }))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeTrue();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeTrue();
        filter(Pirates.Mugiwaras.Sanji).Should().BeTrue();
        filter(Pirates.Mugiwaras.Chopper).Should().BeTrue();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeTrue();
    }

    [Fact]
    public void
        AdvancedFilter_ShouldFilterForAgeGreaterThanOrEqualAndBountyLessThan_IfAgeGreaterThanOrEqualAndBountyLessThan()
    {
        var filter = AdvancedFilterBuilder
            .ForModel<Pirate>()
            .And(b => b.GreaterThanOrEqual(_ => _.Age, 18))
            .And(b => b.LessThan(_ => _.Bounty, 200000000))
            .Build();

        filter(Pirates.Mugiwaras.Luffy).Should().BeFalse();
        filter(Pirates.Mugiwaras.Zoro).Should().BeFalse();
        filter(Pirates.Mugiwaras.Nami).Should().BeTrue();
        filter(Pirates.Mugiwaras.Usopp).Should().BeFalse();
        filter(Pirates.Mugiwaras.Sanji).Should().BeFalse();
        filter(Pirates.Mugiwaras.Chopper).Should().BeFalse();
        filter(Pirates.Mugiwaras.Robin).Should().BeTrue();
        filter(Pirates.Mugiwaras.Franky).Should().BeTrue();
        filter(Pirates.Mugiwaras.Brook).Should().BeTrue();
        filter(Pirates.Mugiwaras.Jinbe).Should().BeFalse();
    }
}