using Raftel.Domain.Specifications;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Specifications;

public class SortMapTests
{
    private sealed record Widget(string Name, DateTime CreatedAt);

    [Fact]
    public void Resolve_Should_Return_Expression_For_An_Allowed_Field()
    {
        var map = new SortMap<Widget>().Allow("name", w => w.Name);

        var result = map.Resolve("name");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public void Resolve_Should_Fail_For_An_Unknown_Field()
    {
        var map = new SortMap<Widget>().Allow("name", w => w.Name);

        var result = map.Resolve("secretInternalColumn");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Sort.UnknownField");
    }

    [Fact]
    public void Resolve_Should_Be_Case_Insensitive()
    {
        var map = new SortMap<Widget>().Allow("name", w => w.Name);

        var result = map.Resolve("NAME");

        result.IsSuccess.ShouldBeTrue();
    }
}
