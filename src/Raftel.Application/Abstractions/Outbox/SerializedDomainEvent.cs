namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// A domain event in its stored form: the stable type name and the JSON content.
/// </summary>
/// <param name="Type">The stored type name, as given by <see cref="IDomainEventTypeRegistry"/>.</param>
/// <param name="Content">The JSON representation of the event.</param>
public sealed record SerializedDomainEvent(string Type, string Content);
