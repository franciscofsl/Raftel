using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Application.Commands;

namespace Raftel.Application.Tests.Commands;

public class CommandDispatcherTests
{
    public class CreateProductCommand : ICommand
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    [Fact]
    public async Task DispatchAsync_Should_Call_CommandHandler_When_CommandIsDispatched()
    {
        var mockHandler = Substitute.For<ICommandHandler<CreateProductCommand>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<CreateProductCommand>)).Returns(mockHandler);

        var commandDispatcher = new CommandDispatcher(serviceProvider);
        var command = new CreateProductCommand { Name = "New Product", Price = 100.0m };

        await commandDispatcher.DispatchAsync(command);

        await mockHandler.Received(1).HandleAsync(command);
    }

    [Fact]
    public async Task DispatchAsync_Should_ThrowException_When_CommandHandlerFails()
    {
        var mockHandler = Substitute.For<ICommandHandler<CreateProductCommand>>();
        mockHandler.HandleAsync(Arg.Any<CreateProductCommand>())
            .Throws(new Exception("Handler failed"));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<CreateProductCommand>)).Returns(mockHandler);

        var commandDispatcher = new CommandDispatcher(serviceProvider);
        var command = new CreateProductCommand { Name = "New Product", Price = 100.0m };

        await Assert.ThrowsAsync<Exception>(() => commandDispatcher.DispatchAsync(command));
    }
}