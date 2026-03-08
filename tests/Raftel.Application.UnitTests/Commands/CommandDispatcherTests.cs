using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Application.UnitTests.Commands;

public class CommandDispatcherTests
{
    public record TestCommand(string Message) : ICommand;

    public record TestCommandWithResult(string Message) : ICommand<string>;

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

    [Fact]
    public async Task DispatchAsync_WithResult_Should_Delegate_To_RequestDispatcher_And_Return_Result()
    {
        var command = new TestCommandWithResult("Raftel");
        var expected = Result.Success("Grand Line");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestCommandWithResult, Result<string>>(command)
            .Returns(expected);

        var dispatcher = new CommandDispatcher(requestDispatcher);

        var result = await dispatcher.DispatchAsync<TestCommandWithResult, string>(command);

        result.ShouldBe(expected);
        await requestDispatcher.Received(1).DispatchAsync<TestCommandWithResult, Result<string>>(command);
    }

    [Fact]
    public async Task DispatchAsync_WithResult_Should_Propagate_Exception_From_RequestDispatcher()
    {
        var command = new TestCommandWithResult("Invalid");

        var requestDispatcher = Substitute.For<IRequestDispatcher>();
        requestDispatcher
            .DispatchAsync<TestCommandWithResult, Result<string>>(command)
            .Returns<Task<Result<string>>>(x => throw new InvalidOperationException("No handler"));

        var dispatcher = new CommandDispatcher(requestDispatcher);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync<TestCommandWithResult, string>(command));

        ex.Message.ShouldBe("No handler");
    }
}