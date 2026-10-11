namespace Raftel.Application;

/// <summary>
/// Options for the transactional outbox that stores domain events with the state change that raised
/// them and delivers them to their handlers asynchronously.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Whether domain events go through the outbox. Defaults to <see langword="false"/>: events are then
    /// dispatched in-process right after <c>SaveChanges</c>, as before the outbox existed.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Maximum number of messages claimed by one processing cycle. Defaults to 50.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>How long the processor waits between polls. Defaults to 5 seconds.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How many delivery attempts a message gets before it becomes a dead letter. Defaults to 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Delay before the first retry; it doubles with each further attempt. Defaults to 10 seconds.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Upper bound for the retry delay. Defaults to 5 minutes.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a processor owns a claimed message before another instance may claim it again. Size it
    /// above the slowest handler. Defaults to 1 minute.
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>How long processed messages are kept before cleanup deletes them. Defaults to 7 days.</summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> naming the first invalid setting.
    /// </summary>
    public void Validate()
    {
        RequirePositive(BatchSize, nameof(BatchSize));
        RequirePositive(MaxAttempts, nameof(MaxAttempts));
        RequirePositive(PollingInterval, nameof(PollingInterval));
        RequirePositive(BaseRetryDelay, nameof(BaseRetryDelay));
        RequirePositive(MaxRetryDelay, nameof(MaxRetryDelay));
        RequirePositive(LockDuration, nameof(LockDuration));
        RequirePositive(RetentionPeriod, nameof(RetentionPeriod));

        if (MaxRetryDelay < BaseRetryDelay)
        {
            throw new InvalidOperationException(
                $"Outbox option '{nameof(MaxRetryDelay)}' ({MaxRetryDelay}) must not be below '{nameof(BaseRetryDelay)}' ({BaseRetryDelay}).");
        }
    }

    private static void RequirePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"Outbox option '{name}' must be greater than zero, but was {value}.");
        }
    }

    private static void RequirePositive(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Outbox option '{name}' must be greater than zero, but was {value}.");
        }
    }
}
