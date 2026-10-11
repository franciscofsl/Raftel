namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// Computes how long a failed outbox message waits before its next delivery attempt.
/// </summary>
public static class OutboxRetrySchedule
{
    /// <summary>
    /// Returns <c>baseDelay * 2^(attempt - 1)</c>, capped at <paramref name="maxDelay"/>.
    /// </summary>
    /// <param name="attempt">The attempt that just failed, starting at 1.</param>
    /// <param name="baseDelay">The delay after the first failed attempt.</param>
    /// <param name="maxDelay">The longest delay ever returned.</param>
    public static TimeSpan DelayFor(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        var exponent = Math.Max(attempt, 1) - 1;
        var ticks = baseDelay.Ticks * Math.Pow(2, exponent);

        return ticks >= maxDelay.Ticks ? maxDelay : TimeSpan.FromTicks((long)ticks);
    }
}
