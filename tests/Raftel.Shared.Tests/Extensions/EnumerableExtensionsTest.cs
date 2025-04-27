using Raftel.Shared.Extensions;
using Shouldly;

namespace Raftel.Shared.Tests.Extensions;

public class EnumerableExtensionsTest
{
    [Fact]
    public void WhereIf_ReturnsFilteredCollection_WhenConditionIsTrue()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var result = source.WhereIf(true, x => x > 3);
        result.ShouldBe(new[] { 4, 5 });
    }

    [Fact]
    public void WhereIf_ReturnsOriginalCollection_WhenConditionIsFalse()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var result = source.WhereIf(false, x => x > 3);
        result.ShouldBe(source);
    }

    [Fact]
    public void WhereIf_ReturnsEmptyCollection_WhenSourceIsEmpty()
    {
        var source = Array.Empty<int>();
        var result = source.WhereIf(true, x => x > 3);
        result.ShouldBeEmpty();
    }

    [Fact]
    public void WhereIf_ThrowsArgumentNullException_WhenSourceIsNull()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.WhereIf(true, x => x > 3));
    }

    [Fact]
    public void WhereIf_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        Should.Throw<ArgumentNullException>(() => source.WhereIf(true, null));
    }
}