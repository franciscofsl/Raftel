using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Options;
using Raftel.Application.Abstractions;

namespace Raftel.Infrastructure.Health;

public sealed class OutboxHealthCheck(
    IOutboxHealthProbe outboxHealthProbe,
    IOptions<HealthOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        TimeSpan? oldestUnprocessedAge;
        int deadLetterCount;
        try
        {
            oldestUnprocessedAge = await outboxHealthProbe.GetOldestUnprocessedMessageAgeAsync(cancellationToken);
            deadLetterCount = await outboxHealthProbe.GetDeadLetterCountAsync(cancellationToken);
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Outbox did not respond.");
        }

        var deadLetterThreshold = options.Value.DeadLetterThreshold;
        if (deadLetterCount > deadLetterThreshold)
        {
            return HealthCheckResult.Unhealthy($"{deadLetterCount} dead-lettered messages (threshold {deadLetterThreshold}).");
        }

        var lagThreshold = options.Value.OutboxLagThreshold;
        if (oldestUnprocessedAge is { } age && age > lagThreshold)
        {
            return HealthCheckResult.Degraded($"Oldest unprocessed message is {age} old (threshold {lagThreshold}).");
        }

        return HealthCheckResult.Healthy();
    }
}
