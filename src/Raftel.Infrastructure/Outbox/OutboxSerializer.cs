using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Stores domain events as JSON and materialises them back, only for event types that have a registered
/// handler. A stored type name is looked up in the registry and never resolved through type loading.
/// </summary>
internal sealed class OutboxSerializer : IOutboxSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private readonly IDomainEventTypeRegistry _registry;

    public OutboxSerializer(IDomainEventTypeRegistry registry)
    {
        _registry = registry;
    }

    public SerializedDomainEvent Serialize(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();

        if (!_registry.TryGetName(eventType, out var name))
        {
            throw new InvalidOperationException(
                $"Domain event '{eventType.FullName}' has no registered handler, so it cannot be stored in the outbox.");
        }

        try
        {
            return new SerializedDomainEvent(name, JsonSerializer.Serialize(domainEvent, eventType, Options));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Domain event '{eventType.FullName}' could not be serialized for the outbox: {exception.Message}",
                exception);
        }
    }

    public Result<IDomainEvent> Deserialize(string type, string content)
    {
        if (!_registry.TryGetEventType(type, out var eventType))
        {
            return Result.Failure<IDomainEvent>(OutboxErrors.UnknownEventType(type));
        }

        try
        {
            var domainEvent = JsonSerializer.Deserialize(content, eventType, Options) as IDomainEvent;

            return domainEvent is null
                ? Result.Failure<IDomainEvent>(OutboxErrors.InvalidContent(type, "the content is empty"))
                : Result.Success(domainEvent);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or NotSupportedException
                                              or FormatException or InvalidOperationException)
        {
            return Result.Failure<IDomainEvent>(OutboxErrors.InvalidContent(type, exception.Message));
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { RestoreGetOnlyProperties }
            }
        };
        options.Converters.Add(new TypedGuidIdJsonConverterFactory());
        return options;
    }

    /// <summary>
    /// Domain events declare values such as <c>OccurredOn { get; } = UtcNow</c>. <c>System.Text.Json</c>
    /// skips get-only properties, so a deserialized event would silently get a fresh timestamp. Write them
    /// through their compiler-generated backing field instead.
    /// </summary>
    private static void RestoreGetOnlyProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.Set is not null || property.Get is null || property.AttributeProvider is not PropertyInfo info)
            {
                continue;
            }

            var backingField = info.DeclaringType?.GetField(
                $"<{info.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

            if (backingField is not null)
            {
                property.Set = backingField.SetValue;
            }
        }
    }
}
