using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Abstractions;

public class PageRequestTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Fail_When_Page_Is_Below_One(int page)
    {
        var result = PageRequest.Create(page);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Page.Invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PageRequest.MaxPageSize + 1)]
    public void Create_Should_Fail_When_PageSize_Is_Out_Of_Bounds(int pageSize)
    {
        var result = PageRequest.Create(pageSize: pageSize);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PageSize.Invalid");
    }

    [Fact]
    public void Create_Should_Succeed_With_Valid_Page_And_PageSize()
    {
        var result = PageRequest.Create(page: 3, pageSize: 10);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Page.ShouldBe(3);
        result.Value.PageSize.ShouldBe(10);
        result.Value.Skip.ShouldBe(20);
    }

    [Fact]
    public void Create_Should_Use_Defaults_When_Not_Specified()
    {
        var result = PageRequest.Create();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(PageRequest.DefaultPageSize);
        result.Value.Skip.ShouldBe(0);
    }
}
