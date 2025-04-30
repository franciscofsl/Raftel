using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Application.UnitTests.Commands;

public class CommandDispatcherTests
{
    public record TestCommand(string Message) : ICommand;

    [Fact]
    public async Task DispatchAsync_Should_Delegate_To_RequestDispatcher()
    {
        var command = new TestCommand("Raftel");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestCommand, Result>(command)
            .Returns(Result.Success());

        var dispatcher = new CommandDispatcher(requestDispatcher);

        var result = await dispatcher.DispatchAsync(command);

        result.IsSuccess.ShouldBeTrue();
        await requestDispatcher.Received(1).DispatchAsync<TestCommand, Result>(command);
    }

    [Fact]
    public async Task DispatchAsync_Should_Propagate_Exception_From_RequestDispatcher()
    {
        var command = new TestCommand("Invalid");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestCommand, Result>(command)
            .Returns<Task<Result>>(x => throw new InvalidOperationException("No handler"));

        var dispatcher = new CommandDispatcher(requestDispatcher);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(command));

        ex.Message.ShouldBe("No handler");
    }
}