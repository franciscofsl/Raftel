using Raftel.Domain.Outbox.ValueObjects;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Records that a handler completed for an outbox message. Written in the same transaction as the
/// handler's own database writes, so a redelivered message skips handlers that already finished.
/// </summary>
internal sealed class OutboxHandlerReceipt
{
    public const int MaxHandlerLength = 512;

    public OutboxHandlerReceipt(OutboxMessageId messageId, string handler, DateTimeOffset completedOn)
    {
        MessageId = messageId;
        Handler = handler;
        CompletedOn = completedOn;
    }

    public OutboxMessageId MessageId { get; private set; }
    public string Handler { get; private set; }
    public DateTimeOffset CompletedOn { get; private set; }
}
