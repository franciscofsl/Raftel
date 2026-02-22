using Raftel.Application.Abstractions;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Queries;

public class PagedQueryDispatcherTests
{
    public sealed record TestPagedQuery(int Page, int PageSize, string Filter)
        : IPagedQuery<string>;

    [Fact]
    public async Task DispatchPagedAsync_Should_Delegate_To_RequestDispatcher_And_Return_PagedResult()
    {
        var query = new TestPagedQuery(1, 10, "Straw Hat");
        var pagedResult = new PagedResult<string>(["Luffy", "Zoro"], 2, 1, 10);
        var expected = Result.Success(pagedResult);

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestPagedQuery, Result<PagedResult<string>>>(query)
            .Returns(expected);

        var dispatcher = new QueryDispatcher(requestDispatcher);

        var result = await dispatcher.DispatchPagedAsync<TestPagedQuery, string>(query);

        result.ShouldBe(expected);
        await requestDispatcher.Received(1)
            .DispatchAsync<TestPagedQuery, Result<PagedResult<string>>>(query);
    }

    [Fact]
    public async Task DispatchPagedAsync_Should_Propagate_Exception_From_RequestDispatcher()
    {
        var query = new TestPagedQuery(1, 10, "Void Century");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestPagedQuery, Result<PagedResult<string>>>(query)
            .Returns<Task<Result<PagedResult<string>>>>(x =>
                throw new InvalidOperationException("Handler missing"));

        var dispatcher = new QueryDispatcher(requestDispatcher);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchPagedAsync<TestPagedQuery, string>(query));

        exception.Message.ShouldBe("Handler missing");
    }

    [Fact]
    public void TestPagedQuery_Should_Implement_IPagedQuery()
    {
        var query = new TestPagedQuery(2, 5, "Test");

        query.Page.ShouldBe(2);
        query.PageSize.ShouldBe(5);
        query.ShouldBeAssignableTo<IPagedQuery<string>>();
        query.ShouldBeAssignableTo<IQuery<PagedResult<string>>>();
    }
}
