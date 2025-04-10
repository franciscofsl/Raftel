using NSubstitute;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.Tests.Queries;

public class QueryDispatcherTests
{
    public record TestQuery(string Input) : IQuery<string>;

    [Fact]
    public async Task DispatchAsync_Should_Invoke_Handler_And_Return_Result()
    {
        var query = new TestQuery("Raftel");

        var expectedResult = Result.Success("Grand Line");

        var handler = Substitute.For<IQueryHandler<TestQuery, string>>();
        handler.HandleAsync(query).Returns(expectedResult);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IQueryHandler<TestQuery, string>))
            .Returns(handler);

        var dispatcher = new QueryDispatcher(serviceProvider);

        var result = await dispatcher.DispatchAsync<TestQuery, string>(query);

        result.ShouldBe(expectedResult);
        await handler.Received(1).HandleAsync(query);
    }

    [Fact]
    public async Task DispatchAsync_Should_Throw_When_Handler_Not_Registered()
    {
        var query = new TestQuery("Void Century");

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IQueryHandler<TestQuery, string>))
            .Returns(null);

        var dispatcher = new QueryDispatcher(serviceProvider);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync<TestQuery, string>(query));

        exception.Message.ShouldContain("No service for type");
    }
}