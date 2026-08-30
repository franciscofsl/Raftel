using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;
using SortDirection = Raftel.Domain.Abstractions.SortDirection;

namespace Raftel.Domain.Tests.Abstractions;

public class SortRequestTests
{
    [Fact]
    public void Parse_Should_Return_Single_Ascending_Entry()
    {
        var result = SortRequest.Parse("name");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([new SortRequest("name", SortDirection.Ascending)]);
    }

    [Fact]
    public void Parse_Should_Return_Single_Descending_Entry()
    {
        var result = SortRequest.Parse("-createdAt");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([new SortRequest("createdAt", SortDirection.Descending)]);
    }

    [Fact]
    public void Parse_Should_Return_Multiple_Entries_In_Order()
    {
        var result = SortRequest.Parse("a,-b");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(
        [
            new SortRequest("a", SortDirection.Ascending),
            new SortRequest("b", SortDirection.Descending)
        ]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Should_Return_Empty_List_When_Raw_Is_Empty_Or_Absent(string raw)
    {
        var result = SortRequest.Parse(raw);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("-")]
    [InlineData("a,,b")]
    [InlineData(" , ")]
    public void Parse_Should_Fail_On_An_Unparseable_Segment(string raw)
    {
        var result = SortRequest.Parse(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Sort.Invalid");
    }
}
