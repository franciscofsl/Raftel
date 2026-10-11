using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Raftel.Infrastructure.Health;

public sealed class StartupHostedService(
    StartupHealthCheck startupHealthCheck,
    IEnumerable<IStartupTask> startupTasks,
    ILogger<StartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var startupTask in startupTasks)
            {
                await startupTask.ExecuteAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Startup task failed; instance will remain not ready.");
            return;
        }

        startupHealthCheck.MarkReady();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
