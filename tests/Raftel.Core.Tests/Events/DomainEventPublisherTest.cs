using Microsoft.Extensions.Configuration;
using NSubstitute;
using Raftel.Core;

namespace Raftel.Core.Tests.Events;

public class DomainEventPublisherTest
{
    private const string ExceptionMessage = "My exception";

    [Fact]
    public async Task Should_Retrieve_Valid_Domain_Event_Handler_By_Event()
    {
        var serviceCollection = new ServiceCollection();
        var dddModule = new DddModule();
        dddModule.ConfigureCustomServices(serviceCollection, Substitute.For<IConfiguration>());
        serviceCollection.AddTransient<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var domainEventPublisher = serviceProvider.GetRequiredService<IDomainEventPublisher>();
        // Act and Assert
        await FluentActions.Awaiting(async () => await domainEventPublisher.Publish(new TestDomainEvent()))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Should_Throw_Domain_Exception_In_Handler()
    {
        var serviceCollection = new ServiceCollection();
        var dddModule = new DddModule();
        dddModule.ConfigureCustomServices(serviceCollection, Substitute.For<IConfiguration>());
        serviceCollection.AddTransient<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var domainEventPublisher = serviceProvider.GetRequiredService<IDomainEventPublisher>();

        var exception = await Assert.ThrowsAsync<DomainEventException>(async () =>
        {
            await domainEventPublisher.Publish(new TestDomainEvent(true));
        });

        exception.Message.Should().Be(ExceptionMessage);
    }

    public record TestDomainEvent(bool ThrowException = false) : IDomainEvent;

    public class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
    {
        public Task Handle(TestDomainEvent domainEvent, CancellationToken token = default)
        {
            if (domainEvent.ThrowException)
            {
                throw new DomainEventException(ExceptionMessage);
            }

            return Task.CompletedTask;
        }
    }
}