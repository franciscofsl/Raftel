using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Outbox;

internal static class OutboxErrors
{
    public static Error UnknownEventType(string type) =>
        Error.Failure("Outbox.UnknownEventType",
            $"'{type}' is not a registered domain event type; the message cannot be delivered.");

    public static Error InvalidContent(string type, string reason) =>
        Error.Failure("Outbox.InvalidContent", $"The content of a '{type}' message could not be read: {reason}");
}
