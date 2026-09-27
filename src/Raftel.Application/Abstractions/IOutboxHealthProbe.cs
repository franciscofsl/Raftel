namespace Raftel.Application.Abstractions;

public interface IOutboxHealthProbe
{
    Task<TimeSpan?> GetOldestUnprocessedMessageAgeAsync(CancellationToken cancellationToken);

    Task<int> GetDeadLetterCountAsync(CancellationToken cancellationToken);
}
