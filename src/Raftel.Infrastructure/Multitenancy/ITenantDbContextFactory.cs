using Microsoft.EntityFrameworkCore;

namespace Raftel.Infrastructure.Multitenancy;

/// <summary>
/// Factory for creating DbContext instances with tenant-specific connection strings.
/// </summary>
public interface ITenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Creates a DbContext instance for the current tenant.
    /// If the current tenant has a specific connection string, it will be used.
    /// Otherwise, the default connection string will be used.
    /// </summary>
    /// <returns>A DbContext instance configured for the current tenant.</returns>
    Task<TDbContext> CreateDbContextAsync();

    /// <summary>
    /// Creates a DbContext instance for a specific tenant.
    /// If the tenant has a specific connection string, it will be used.
    /// Otherwise, the default connection string will be used.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <returns>A DbContext instance configured for the specified tenant.</returns>
    Task<TDbContext> CreateDbContextAsync(Guid tenantId);
}