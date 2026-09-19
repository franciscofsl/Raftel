namespace Raftel.Infrastructure.Health;

public interface IDatabaseProbe
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken);
}
