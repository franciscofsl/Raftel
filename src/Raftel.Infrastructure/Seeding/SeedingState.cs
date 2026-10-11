namespace Raftel.Infrastructure.Seeding;

public enum SeedingStatus
{
    NotRequested,
    Running,
    Completed,
    CompletedWithFailures,
    Failed
}

/// <summary>
/// Tracks the outcome of startup seeding so the readiness probe can reflect it.
/// </summary>
public sealed class SeedingState
{
    private volatile SeedingStatus _status = SeedingStatus.NotRequested;

    public SeedingStatus Status => _status;

    public void MarkRunning() => _status = SeedingStatus.Running;

    public void MarkCompleted() => _status = SeedingStatus.Completed;

    public void MarkCompletedWithFailures() => _status = SeedingStatus.CompletedWithFailures;

    public void MarkFailed() => _status = SeedingStatus.Failed;
}
