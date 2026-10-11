using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.Events;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Outbox;

public class DomainEventTypeRegistryTests
{
    private const string CrownedKingName =
        "Raftel.Demo.Domain.Pirates.Events.PirateCrownedKing, Raftel.Demo.Domain";

    [Fact]
    public void Registry_ShouldMapHandledEventType_ToVersionlessName()
    {
        var registry = BuildRegistry();

        registry.TryGetName(typeof(PirateCrownedKing), out var name).ShouldBeTrue();
        name.ShouldBe(CrownedKingName);
    }

    [Fact]
    public void Registry_ShouldResolveName_BackToEventType()
    {
        var registry = BuildRegistry();

        registry.TryGetEventType(CrownedKingName, out var eventType).ShouldBeTrue();
        eventType.ShouldBe(typeof(PirateCrownedKing));
    }

    [Fact]
    public void Registry_ShouldListEveryHandlerImplementation_ForTheEvent()
    {
        var registry = BuildRegistry();

        registry.GetHandlerTypes(typeof(PirateCrownedKing)).ShouldBe([typeof(PirateCrownedKingHandler)]);
    }

    [Fact]
    public void Registry_ShouldReportUnhandledEventType_AsNotRegistered()
    {
        var registry = BuildRegistry();

        registry.TryGetName(typeof(UnhandledEvent), out _).ShouldBeFalse();
        registry.GetHandlerTypes(typeof(UnhandledEvent)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Does.Not.Exist, Nowhere")]
    [InlineData("System.IO.File, System.Runtime")]
    [InlineData("")]
    public void Registry_ShouldReportUnknownName_AsNotRegistered(string name)
    {
        var registry = BuildRegistry();

        registry.TryGetEventType(name, out var eventType).ShouldBeFalse();
        eventType.ShouldBeNull();
    }

    private static IDomainEventTypeRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly));

        return services.BuildServiceProvider().GetRequiredService<IDomainEventTypeRegistry>();
    }

    private sealed record UnhandledEvent : IDomainEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }
}
