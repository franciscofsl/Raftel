using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;
using Raftel.Infrastructure.Data.Interceptors;

namespace Raftel.Infrastructure.Multitenancy;

/// <summary>
/// Factory for creating DbContext instances with tenant-specific connection strings.
/// </summary>
internal sealed class TenantDbContextFactory<TDbContext> : ITenantDbContextFactory<TDbContext>
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ICurrentTenant _currentTenant;
    private readonly string _defaultConnectionString;

    public TenantDbContextFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ICurrentTenant currentTenant)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _currentTenant = currentTenant;
        _defaultConnectionString = configuration.GetConnectionString("Default")
                                  ?? throw new InvalidOperationException("Default connection string not found.");
    }

    public async Task<TDbContext> CreateDbContextAsync()
    {
        if (_currentTenant.Id.HasValue)
        {
            return await CreateDbContextAsync(_currentTenant.Id.Value);
        }

        return CreateDbContextWithConnectionString(_defaultConnectionString);
    }

    public async Task<TDbContext> CreateDbContextAsync(Guid tenantId)
    {
        // Get tenant-specific connection string
        var connectionString = await GetTenantConnectionStringAsync(tenantId);
        return CreateDbContextWithConnectionString(connectionString ?? _defaultConnectionString);
    }

    private async Task<string?> GetTenantConnectionStringAsync(Guid tenantId)
    {
        // Create a temporary context using the default connection to retrieve the tenant
        using var scope = _serviceProvider.CreateScope();
        var tempContext = CreateDbContextWithConnectionString(_defaultConnectionString);
        
        try
        {
            var tenant = await tempContext.Tenant.FindAsync(tenantId);
            return tenant?.GetConnectionString();
        }
        finally
        {
            await tempContext.DisposeAsync();
        }
    }

    private TDbContext CreateDbContextWithConnectionString(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseSqlServer(connectionString)
                     .UseOpenIddict()
                     .AddInterceptors(
                         _serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                         _serviceProvider.GetRequiredService<TenantInterceptor>());

        // Create the context with proper dependency injection
        var dataFilter = _serviceProvider.GetRequiredService<IDataFilter>();
        var currentTenant = _serviceProvider.GetRequiredService<ICurrentTenant>();

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options, dataFilter, currentTenant)!;
    }
}