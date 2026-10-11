namespace Raftel.Infrastructure.Outbox;

internal static class OutboxRegistrationException
{
    public static InvalidOperationException MissingEventTypeRegistry() =>
        new("The outbox is enabled but no domain event type registry is registered. " +
            "Call AddRaftelApplication before using the outbox, so that event handlers can be discovered.");
}
