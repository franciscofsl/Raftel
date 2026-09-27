using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Hosting;
using Raftel.Application.Abstractions;

namespace Raftel.Infrastructure.Health;

public static class HealthDependencyInjection
{
    public static IServiceCollection AddRaftelHealthChecks(
        this IServiceCollection services,
        Action<HealthOptions> configureOptions = null)
    {
        var options = new HealthOptions();
        configureOptions?.Invoke(options);

        if (options.EnableOutboxCheck && !services.Any(d => d.ServiceType == typeof(IOutboxHealthProbe)))
        {
            throw new InvalidOperationException(
                $"{nameof(HealthOptions.EnableOutboxCheck)} is enabled but no {nameof(IOutboxHealthProbe)} " +
                "is registered. Register an implementation before enabling the outbox health check.");
        }

        services.Configure<HealthOptions>(o =>
        {
            o.RequireAuthorizationForDetails = options.RequireAuthorizationForDetails;
            o.DetailsPolicy = options.DetailsPolicy;
            o.CheckTimeout = options.CheckTimeout;
            o.DatabaseSlowThreshold = options.DatabaseSlowThreshold;
            o.OutboxLagThreshold = options.OutboxLagThreshold;
            o.DeadLetterThreshold = options.DeadLetterThreshold;
            o.EnableTenantResolutionCheck = options.EnableTenantResolutionCheck;
            o.EnableOutboxCheck = options.EnableOutboxCheck;
        });

        services.AddSingleton<StartupHealthCheck>();
        services.AddHostedService<StartupHostedService>();

        var builder = services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(HealthCheckNames.Database, tags: [HealthCheckTags.Ready], timeout: options.CheckTimeout)
            .AddCheck<PendingMigrationsHealthCheck>(HealthCheckNames.Migrations, tags: [HealthCheckTags.Ready], timeout: options.CheckTimeout)
            .AddCheck<StartupHealthCheck>(HealthCheckNames.Startup, tags: [HealthCheckTags.Ready], timeout: options.CheckTimeout);

        if (options.EnableTenantResolutionCheck)
        {
            builder.AddCheck<TenantResolutionHealthCheck>(HealthCheckNames.Tenants, tags: [HealthCheckTags.Ready], timeout: options.CheckTimeout);
        }

        if (options.EnableOutboxCheck)
        {
            builder.AddCheck<OutboxHealthCheck>(HealthCheckNames.Outbox, tags: [HealthCheckTags.Ready], timeout: options.CheckTimeout);
        }

        return services;
    }
}
