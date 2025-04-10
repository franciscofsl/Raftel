using NSubstitute;
using Raftel.Application.Commands;
using Shouldly;

namespace Raftel.Application.Tests.Commands;

public class CommandDispatcherTests
{
    // ReSharper disable once MemberCanBePrivate.Global
    public record TestCommand(string Message) : ICommand;

    [Fact]
    public async Task DispatchAsync_Should_Invoke_Handler()
    {
        var command = new TestCommand("Raftel");

        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<TestCommand>)).Returns(handler);

        var dispatcher = new CommandDispatcher(serviceProvider);

        await dispatcher.DispatchAsync(command);

        await handler.Received(1).HandleAsync(command);
    }

    [Fact]
    public async Task DispatchAsync_Should_Throw_When_Handler_Not_Registered()
    {
        var command = new TestCommand("Command without handler");

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<TestCommand>)).Returns(null);

        var dispatcher = new CommandDispatcher(serviceProvider);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(command));

        exception.Message.ShouldContain("No service for type");
    }
}