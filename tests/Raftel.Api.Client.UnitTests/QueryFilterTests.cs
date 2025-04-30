using Shouldly;

namespace Raftel.Api.Client.UnitTests;

public class QueryFilterTests
{
    private class TestQuery
    {
        public string Name { get; set; }
        public int? Age { get; set; }
        public int? Bounty { get; set; }
        public DateTime? BirthDate { get; set; }
        public string EmptyString { get; set; }
        public Status? Status { get; set; }
        public List<string> CrewMembers { get; set; }
        public Name EncapsulatedName { get; set; }
    }

    private enum Status
    {
        Active,
        Inactive
    }

    private class Name
    {
        public string Value { get; }

        public Name(string value)
        {
            Value = value;
        }
    }

    [Fact]
    public void ToString_WhenAllPropertiesAreNull_ShouldReturnEmptyString()
    {
        var query = new TestQuery();

        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_WhenSomePropertiesAreSet_ShouldReturnQueryStringWithThoseProperties()
    {
        var query = new TestQuery
        {
            Name = "Luffy",
            Bounty = 1500000000
        };

        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldBe("?bounty=1500000000&name=Luffy");
    }

    [Fact]
    public void ToString_WhenEmptyStringPropertyIsSet_ShouldIncludePropertyWithEmptyValue()
    {
        var query = new TestQuery
        {
            EmptyString = string.Empty
        };
        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldBe("?emptyString=");
    }

    [Fact]
    public void ToString_WhenObjectIsNull_ShouldReturnEmptyString()
    {
        var filter = QueryFilter.FromObject(null).ToString();

        filter.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_WhenDateTimePropertyIsSet_ShouldReturnFormattedQueryString()
    {
        var birthDate = new DateTime(1999, 5, 5);
        var query = new TestQuery
        {
            BirthDate = birthDate
        };

        var filter = QueryFilter.FromObject(query).ToString();

        var expectedFormattedDate = Uri.EscapeDataString(birthDate.ToString("o"));
        filter.ShouldContain($"birthDate={expectedFormattedDate}");
    }

    [Fact]
    public void ToString_WhenEnumPropertyIsSet_ShouldReturnEnumAsString()
    {
        var query = new TestQuery
        {
            Status = Status.Active
        };

        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldContain("status=Active");
    }

    [Fact]
    public void ToString_WhenCollectionPropertyIsSet_ShouldReturnCommaSeparatedValues()
    {
        var query = new TestQuery
        {
            CrewMembers = new List<string> { "Luffy", "Zoro", "Nami" }
        };

        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldContain("?crewMembers=Luffy%2cZoro%2cNami");
    }

    [Fact]
    public void ToString_WhenEncapsulatedPropertyIsSet_ShouldExtractInnerValue()
    {
        var query = new TestQuery
        {
            EncapsulatedName = new Name("Zoro")
        };

        var filter = QueryFilter.FromObject(query).ToString();

        filter.ShouldContain("encapsulatedName=Zoro");
    }
}