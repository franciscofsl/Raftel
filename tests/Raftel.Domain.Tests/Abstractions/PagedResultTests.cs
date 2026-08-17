using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Abstractions;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_Should_Round_Up_When_TotalCount_Has_Remainder()
    {
        var result = new PagedResult<int>([1, 2, 3, 4, 5], page: 1, pageSize: 5, totalCount: 11);

        result.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void HasPrevious_And_HasNext_Should_Be_False_On_The_Only_Page()
    {
        var result = new PagedResult<int>([1, 2, 3], page: 1, pageSize: 5, totalCount: 3);

        result.HasPrevious.ShouldBeFalse();
        result.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void HasPrevious_Should_Be_False_And_HasNext_True_On_First_Page()
    {
        var result = new PagedResult<int>([1, 2, 3, 4, 5], page: 1, pageSize: 5, totalCount: 11);

        result.HasPrevious.ShouldBeFalse();
        result.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void HasPrevious_And_HasNext_Should_Both_Be_True_On_Intermediate_Page()
    {
        var result = new PagedResult<int>([6, 7, 8, 9, 10], page: 2, pageSize: 5, totalCount: 11);

        result.HasPrevious.ShouldBeTrue();
        result.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void HasNext_Should_Be_False_And_HasPrevious_True_On_Last_Page()
    {
        var result = new PagedResult<int>([11], page: 3, pageSize: 5, totalCount: 11);

        result.HasPrevious.ShouldBeTrue();
        result.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void Empty_Should_Produce_A_Page_With_No_Items_And_No_Total()
    {
        var request = PageRequest.Create(page: 2, pageSize: 5).Value;

        var result = PagedResult<int>.Empty(request);

        result.Items.ShouldBeEmpty();
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(5);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void Map_Should_Project_Items_While_Preserving_Paging_Metadata()
    {
        var result = new PagedResult<int>([1, 2, 3], page: 1, pageSize: 3, totalCount: 3);

        var mapped = result.Map(value => value.ToString());

        mapped.Items.ShouldBe(["1", "2", "3"]);
        mapped.Page.ShouldBe(1);
        mapped.PageSize.ShouldBe(3);
        mapped.TotalCount.ShouldBe(3);
    }
}
