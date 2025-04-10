using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.Tests.Queries;

public class QueryDispatcherTests
{
    public record TestQuery(string Input) : IQuery<string>;

    [Fact]
    public async Task DispatchAsync_Should_Delegate_To_RequestDispatcher_And_Return_Result()
    {
        var query = new TestQuery("Raftel");
        var expected = Result.Success("Grand Line");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestQuery, Result<string>>(query)
            .Returns(expected);

        var dispatcher = new QueryDispatcher(requestDispatcher);

        var result = await dispatcher.DispatchAsync<TestQuery, string>(query);

        result.ShouldBe(expected);
        await requestDispatcher.Received(1).DispatchAsync<TestQuery, Result<string>>(query);
    }

    [Fact]
    public async Task DispatchAsync_Should_Propagate_Exception_From_RequestDispatcher()
    {
        var query = new TestQuery("Void Century");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestQuery, Result<string>>(query)
            .Returns<Task<Result<string>>>(x => throw new InvalidOperationException("Handler missing"));

        var dispatcher = new QueryDispatcher(requestDispatcher);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync<TestQuery, string>(query));

        exception.Message.ShouldBe("Handler missing");
    }
}