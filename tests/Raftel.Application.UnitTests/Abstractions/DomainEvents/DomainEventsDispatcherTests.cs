using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.DomainEvents;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions.DomainEvents;

public sealed class DomainEventsDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ShouldInvokeRegisteredHandler_ForEventType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddTransient<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();

        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventsDispatcher(provider);

        await dispatcher.DispatchAsync([new TestDomainEvent("Luffy is King")]);

        var spy = provider.GetRequiredService<ISpy>();
        spy.InterceptedMessages().ShouldContain("Handler1: Luffy is King");
    }

    [Fact]
    public async Task DispatchAsync_ShouldInvokeAllRegisteredHandlers_WhenMultipleHandlersExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddTransient<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();
        services.AddTransient<IDomainEventHandler<TestDomainEvent>, TestDomainEventSecondHandler>();

        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventsDispatcher(provider);

        await dispatcher.DispatchAsync([new TestDomainEvent("Luffy is King")]);

        var spy = provider.GetRequiredService<ISpy>();
        spy.InterceptedMessages().ShouldContain("Handler1: Luffy is King");
        spy.InterceptedMessages().ShouldContain("Handler2: Luffy is King");
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotFail_WhenEventHasNoHandler()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventsDispatcher(provider);

        await Should.NotThrowAsync(() => dispatcher.DispatchAsync([new UnhandledTestDomainEvent()]));
    }
}
