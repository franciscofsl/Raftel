using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Seeding;

public static class HostSeedingExtensions
{
    /// <summary>
    /// Applies pending migrations (when enabled) and runs every seeder (when enabled), reporting
    /// progress to the readiness probe. Call it before <c>app.Run()</c>.
    /// </summary>
    /// <exception cref="SeedingFailedException">
    /// Seeding failed and <see cref="SeedOptions.FailFastOnError"/> is enabled.
    /// </exception>
    public static async Task MigrateAndSeedAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        var options = host.Services.GetRequiredService<IOptions<SeedOptions>>().Value;
        var state = host.Services.GetRequiredService<SeedingState>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Raftel.Seeding");

        state.MarkRunning();
        try
        {
            using var scope = host.Services.CreateScope();
            var result = await RunAsync(scope.ServiceProvider, options, cancellationToken);

            if (result.IsSuccess)
            {
                state.MarkCompleted();
                return;
            }

            logger.LogError("Startup seeding failed: {Error}", result.Error.Message);
            if (options.FailFastOnError)
            {
                state.MarkFailed();
                throw new SeedingFailedException(result.Error);
            }

            state.MarkCompletedWithFailures();
        }
        catch (Exception exception) when (exception is not SeedingFailedException)
        {
            state.MarkFailed();
            throw;
        }
    }

    private static async Task<Result> RunAsync(
        IServiceProvider services,
        SeedOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ApplyMigrationsOnStartup)
        {
            await services.GetRequiredService<IDatabaseMigrator>().MigrateAsync(cancellationToken);
        }

        if (!options.RunSeedersOnStartup)
        {
            return Result.Success();
        }

        if (!options.ApplyMigrationsOnStartup)
        {
            var pending = await services.GetRequiredService<IDatabaseProbe>().GetPendingMigrationsAsync(cancellationToken);
            if (pending.Count > 0)
            {
                return Result.Failure(SeedErrors.MigrationsPending(pending));
            }
        }

        return await services.GetRequiredService<ISeedRunner>().RunAsync(cancellationToken);
    }
}
