using Microsoft.Extensions.DependencyInjection;
using Raftel.Domain.Features.Audit;
using Raftel.Infrastructure.Data.Audit;
using Raftel.Infrastructure.Data.Interceptors;
using Raftel.Infrastructure.Data.Repositories.Audit;

namespace Raftel.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for configuring audit functionality.
/// </summary>
public static class AuditExtensions
{
    /// <summary>
    /// Adds audit services to the dependency injection container with a specific DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure audit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAudit<TDbContext>(
        this IServiceCollection services,
        Action<AuditableEntitiesOptions>? configureOptions = null)
        where TDbContext : RaftelDbContext<TDbContext>
    {
        var auditOptions = new AuditableEntitiesOptions();
        configureOptions?.Invoke(auditOptions);

        services.AddSingleton(auditOptions);
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<IAuditRepository, AuditRepository<TDbContext>>();

        return services;
    }
}