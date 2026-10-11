using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// Converts domain events to and from the form stored in the outbox.
/// </summary>
public interface IOutboxSerializer
{
    /// <summary>
    /// Serializes <paramref name="domainEvent"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The event type has no registered handler or cannot be serialized; the message names the type.
    /// </exception>
    SerializedDomainEvent Serialize(IDomainEvent domainEvent);

    /// <summary>
    /// Materialises a stored event. A type name that is not an allow-listed event type, or content that
    /// does not match the type, yields a failure rather than an exception, and never loads the named type.
    /// </summary>
    Result<IDomainEvent> Deserialize(string type, string content);
}
