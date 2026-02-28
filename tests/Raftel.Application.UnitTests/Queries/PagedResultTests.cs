using Raftel.Application.Queries;
using Shouldly;

namespace Raftel.Application.UnitTests.Queries;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_Should_Store_Items_And_Pagination_Properties()
    {
        var items = new List<string> { "Luffy", "Zoro", "Nami" };

        var result = new PagedResult<string>(items, 10, 1, 3);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(10);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(3);
    }

    [Fact]
    public void PagedResult_With_Same_Values_Should_Be_Equal()
    {
        var items = new List<string> { "Luffy" };

        var result1 = new PagedResult<string>(items, 1, 1, 10);
        var result2 = new PagedResult<string>(items, 1, 1, 10);

        result1.ShouldBe(result2);
    }

    [Fact]
    public void PagedResult_With_Different_Pages_Should_Not_Be_Equal()
    {
        var items = new List<string> { "Luffy" };

        var result1 = new PagedResult<string>(items, 1, 1, 10);
        var result2 = new PagedResult<string>(items, 1, 2, 10);

        result1.ShouldNotBe(result2);
    }

    [Fact]
    public void PagedResult_With_Empty_Items_Should_Have_Zero_TotalCount()
    {
        var result = new PagedResult<string>([], 0, 1, 10);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void PagedResult_Should_Throw_When_Page_Is_Zero()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new PagedResult<string>([], 0, 0, 10));
    }

    [Fact]
    public void PagedResult_Should_Throw_When_PageSize_Is_Zero()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new PagedResult<string>([], 0, 1, 0));
    }

    [Fact]
    public void PagedResult_Should_Throw_When_TotalCount_Is_Negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new PagedResult<string>([], -1, 1, 10));
    }

    [Fact]
    public void PagedResult_Should_Throw_When_Items_Is_Null()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PagedResult<string>(null!, 0, 1, 10));
    }
}
