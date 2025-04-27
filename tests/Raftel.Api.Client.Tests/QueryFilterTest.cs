using Shouldly;

namespace Raftel.Api.Client.Tests;

public class QueryFilterTest
{
    private class TestQuery
    {
        public string Name { get; set; }
        public int? Age { get; set; }
        public int? Bounty { get; set; }
        public DateTime? BirthDate { get; set; }
        public string EmptyString { get; set; }
    }

    [Fact]
    public void Build_ObjectWithAllPropertiesNull_ReturnsEmptyString()
    {
        var query = new TestQuery();

        var result = QueryFilter.FromObject(query).Build();

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Build_ObjectWithSomePropertiesSet_ReturnsCorrectQueryString()
    {
        var query = new TestQuery
        {
            Name = "Luffy",
            Bounty = 1500000000
        };

        var result = QueryFilter.FromObject(query).Build();

        result.ShouldBe("?Name=Luffy&Bounty=1500000000");
    }

    [Fact]
    public void Build_ObjectWithEmptyStringProperty_IncludesEmptyValue()
    {
        var query = new TestQuery
        {
            EmptyString = string.Empty
        };

        var result = QueryFilter.FromObject(query).Build();

        result.ShouldBe("?EmptyString=");
    }

    [Fact]
    public void Build_ObjectIsNull_ReturnsEmptyString()
    {
        var result = QueryFilter.FromObject(null).Build();

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Build_ObjectWithDateTimeProperty_ReturnsCorrectFormattedQueryString()
    {
        var birthDate = new DateTime(1999, 5, 5);
        var query = new TestQuery
        {
            BirthDate = birthDate
        };

        var result = QueryFilter.FromObject(query).Build();

        result.ShouldContain($"BirthDate={Uri.EscapeDataString(birthDate.ToString("o"))}");
    }
}