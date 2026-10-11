using Raftel.Domain.BaseTypes;
using Raftel.Domain.Outbox.ValueObjects;

namespace Raftel.Domain.Outbox;

/// <summary>
/// A domain event stored in the same transaction as the state change that raised it, waiting to be
/// delivered to its handlers by the outbox processor.
/// </summary>
/// <remarks>
/// The tenant and correlation identifier are captured because delivery runs outside the originating
/// request, where no ambient tenant or correlation exists. A message that is unprocessed and has used
/// all its attempts is a dead letter.
/// </remarks>
public sealed class OutboxMessage : Entity<OutboxMessageId>
{
    /// <summary>The longest error text stored; longer errors are truncated.</summary>
    public const int MaxErrorLength = 4000;

    private OutboxMessage(
        OutboxMessageId id,
        string type,
        string content,
        DateTimeOffset occurredOn,
        Guid? tenantId,
        string? correlationId) : base(id)
    {
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
        TenantId = tenantId;
        CorrelationId = correlationId;
    }

    private OutboxMessage()
    {
    }

    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public int AttemptCount { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset? NextAttemptOn { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? CorrelationId { get; private set; }

    public static OutboxMessage Create(
        string type,
        string content,
        DateTimeOffset occurredOn,
        Guid? tenantId,
        string? correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(content);

        return new OutboxMessage(OutboxMessageId.New(), type, content, occurredOn, tenantId, correlationId);
    }

    /// <summary>
    /// Leases the message until <paramref name="lockedUntil"/> and counts the delivery attempt up front,
    /// so a message that crashes its processor still runs out of attempts.
    /// </summary>
    public void Claim(DateTimeOffset lockedUntil)
    {
        LockedUntil = lockedUntil;
        AttemptCount++;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedOn = now;
        Error = null;
        NextAttemptOn = null;
        LockedUntil = null;
    }

    public void MarkFailed(DateTimeOffset now, string error, DateTimeOffset nextAttemptOn)
    {
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        NextAttemptOn = nextAttemptOn;
        LockedUntil = null;
    }

    /// <summary>
    /// Gives up the lease without an outcome, handing back the attempt that was counted at claim time.
    /// </summary>
    public void Release()
    {
        LockedUntil = null;
        AttemptCount = Math.Max(0, AttemptCount - 1);
    }

    public bool IsDeadLetter(int maxAttempts) => ProcessedOn is null && AttemptCount >= maxAttempts;
}
