namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// The domain event types that have at least one registered handler, and the handlers for each.
/// It is the allow-list for the outbox: only these types are ever stored or materialised.
/// </summary>
/// <remarks>
/// Built while <c>AddRaftelApplication</c> scans the registered assemblies, so handlers registered by
/// hand outside that scan are not visible here.
/// </remarks>
public interface IDomainEventTypeRegistry
{
    /// <summary>
    /// Gets the stable stored name of <paramref name="eventType"/> (<c>Namespace.Type, Assembly</c>,
    /// without version, culture or key token), if the type has a registered handler.
    /// </summary>
    bool TryGetName(Type eventType, out string name);

    /// <summary>
    /// Resolves a stored name to its event type. Never loads a type that is not registered.
    /// </summary>
    bool TryGetEventType(string name, out Type eventType);

    /// <summary>
    /// Gets the handler implementation types registered for <paramref name="eventType"/>, or an empty
    /// list when there are none.
    /// </summary>
    IReadOnlyList<Type> GetHandlerTypes(Type eventType);
}
