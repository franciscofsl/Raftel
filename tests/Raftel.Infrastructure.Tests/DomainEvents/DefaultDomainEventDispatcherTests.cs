using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raftel.Application.DomainEvents;
using Raftel.Core.BaseTypes;
using Raftel.Infrastructure.DomainEvents;

namespace Raftel.Infrastructure.Tests.DomainEvents;

public class DefaultDomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Should_Invoke_EventHandlers_ForEachEvent()
    {
        var serviceCollection = new ServiceCollection();

        var handler = Substitute.For<IDomainEventHandler<SampleDomainEvent>>();
        serviceCollection.AddSingleton(handler);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var dispatcher = new DefaultDomainEventDispatcher(serviceProvider);

        var domainEvent = new SampleDomainEvent { EventData = "Test Data" };
        var events = new List<IDomainEvent> { domainEvent };

        await dispatcher.DispatchAsync(events, CancellationToken.None);

        await handler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_Should_Invoke_Multiple_Handlers_For_Same_Event()
    {
        var serviceCollection = new ServiceCollection();

        var handler1 = Substitute.For<IDomainEventHandler<SampleDomainEvent>>();
        var handler2 = Substitute.For<IDomainEventHandler<SampleDomainEvent>>();

        serviceCollection.AddSingleton(handler1);
        serviceCollection.AddSingleton(handler2);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var dispatcher = new DefaultDomainEventDispatcher(serviceProvider);

        var domainEvent = new SampleDomainEvent { EventData = "Test Data" };
        var events = new List<IDomainEvent> { domainEvent };

        await dispatcher.DispatchAsync(events, CancellationToken.None);

        await handler1.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Multiple_Events_Correctly()
    {
        var serviceCollection = new ServiceCollection();

        var handler = Substitute.For<IDomainEventHandler<SampleDomainEvent>>();
        serviceCollection.AddSingleton(handler);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var dispatcher = new DefaultDomainEventDispatcher(serviceProvider);

        var event1 = new SampleDomainEvent { EventData = "Event 1 Data" };
        var event2 = new SampleDomainEvent { EventData = "Event 2 Data" };
        var events = new List<IDomainEvent> { event1, event2 };

        await dispatcher.DispatchAsync(events, CancellationToken.None);

        await handler.Received(1).HandleAsync(event1, Arg.Any<CancellationToken>());
        await handler.Received(1).HandleAsync(event2, Arg.Any<CancellationToken>());
    }
}