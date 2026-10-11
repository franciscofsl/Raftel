using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public class OutboxSerializerTests
{
    private readonly OutboxSerializer _serializer;

    public OutboxSerializerTests()
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(CyclicTestEvent).Assembly);
        });

        var registry = services.BuildServiceProvider().GetRequiredService<IDomainEventTypeRegistry>();
        _serializer = new OutboxSerializer(registry);
    }

    [Fact]
    public void RoundTrip_ShouldYieldAnEqualEvent_WithStronglyTypedIdAndOriginalInstant()
    {
        var original = new PirateCrownedKing(PirateId.New());

        var stored = _serializer.Serialize(original);
        var result = _serializer.Deserialize(stored.Type, stored.Content);

        result.IsSuccess.ShouldBeTrue();
        var restored = result.Value.ShouldBeOfType<PirateCrownedKing>();
        restored.PirateId.ShouldBe(original.PirateId);
        restored.OccurredOn.ShouldBe(original.OccurredOn);
    }

    [Fact]
    public async Task RoundTrip_ShouldKeepTheOriginalInstant_NotTheTimeOfDeserialization()
    {
        var original = new PirateCrownedKing(PirateId.New());
        var stored = _serializer.Serialize(original);

        await Task.Delay(20);
        var restored = (PirateCrownedKing)_serializer.Deserialize(stored.Type, stored.Content).Value;

        restored.OccurredOn.ShouldBe(original.OccurredOn);
    }

    [Fact]
    public void Serialize_ShouldStoreAVersionlessTypeName()
    {
        var stored = _serializer.Serialize(new PirateCrownedKing(PirateId.New()));

        stored.Type.ShouldBe("Raftel.Demo.Domain.Pirates.Events.PirateCrownedKing, Raftel.Demo.Domain");
        stored.Type.ShouldNotContain("Version=");
        stored.Type.ShouldNotContain("PublicKeyToken");
    }

    [Fact]
    public void Deserialize_ShouldFail_ForAnUnknownTypeName_WithoutCreatingAnyInstance()
    {
        var result = _serializer.Deserialize("System.Diagnostics.Process, System.Diagnostics.Process", "{}");

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("System.Diagnostics.Process");
    }

    [Fact]
    public void Deserialize_ShouldFail_ForAKnownTypeThatHasNoHandler()
    {
        var result = _serializer.Deserialize(
            "Raftel.Infrastructure.Tests.Outbox.UnhandledTestEvent, Raftel.Infrastructure.Tests", "{}");

        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"PirateId\":\"not-a-guid\"}")]
    [InlineData("{\"PirateId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("")]
    [InlineData("null")]
    public void Deserialize_ShouldFailWithoutThrowing_ForMalformedContent(string content)
    {
        var result = _serializer.Deserialize(
            "Raftel.Demo.Domain.Pirates.Events.PirateCrownedKing, Raftel.Demo.Domain", content);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Serialize_ShouldThrowNamingTheType_WhenTheEventHasNoRegisteredHandler()
    {
        var exception = Should.Throw<InvalidOperationException>(() => _serializer.Serialize(new UnhandledTestEvent()));

        exception.Message.ShouldContain(nameof(UnhandledTestEvent));
    }

    [Fact]
    public void Serialize_ShouldThrowNamingTheType_WhenTheEventCannotBeSerialized()
    {
        var exception = Should.Throw<InvalidOperationException>(() => _serializer.Serialize(new CyclicTestEvent()));

        exception.Message.ShouldContain(nameof(CyclicTestEvent));
    }
}
