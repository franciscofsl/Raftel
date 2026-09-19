namespace Raftel.Infrastructure.Health;

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
