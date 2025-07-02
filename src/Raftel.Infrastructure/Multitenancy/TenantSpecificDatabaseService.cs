using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Multitenancy;

/// <summary>
/// Implementation that demonstrates tenant-specific database operations.
/// </summary>
public sealed class TenantSpecificDatabaseService : ITenantSpecificDatabaseService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantsRepository _tenantsRepository;
    private readonly IConfiguration _configuration;
    private readonly string _defaultConnectionString;

    public TenantSpecificDatabaseService(
        ICurrentTenant currentTenant,
        ITenantsRepository tenantsRepository,
        IConfiguration configuration)
    {
        _currentTenant = currentTenant;
        _tenantsRepository = tenantsRepository;
        _configuration = configuration;
        _defaultConnectionString = configuration.GetConnectionString("Default")
                                  ?? throw new InvalidOperationException("Default connection string not found.");
    }

    public async Task<TenantDatabaseInfo> GetTenantDatabaseInfoAsync()
    {
        var tenantConnectionString = await GetTenantSpecificConnectionStringAsync();
        var effectiveConnectionString = tenantConnectionString ?? _defaultConnectionString;
        var isUsingTenantSpecific = tenantConnectionString != null;

        // Extract database name from connection string
        var databaseName = ExtractDatabaseNameFromConnectionString(effectiveConnectionString);

        return new TenantDatabaseInfo(
            _currentTenant.Id,
            effectiveConnectionString,
            isUsingTenantSpecific,
            databaseName);
    }

    private async Task<string?> GetTenantSpecificConnectionStringAsync()
    {
        if (!_currentTenant.Id.HasValue)
        {
            return null;
        }

        var tenantId = new Domain.Features.Tenants.ValueObjects.TenantId(_currentTenant.Id.Value);
        var tenant = await _tenantsRepository.GetByIdAsync(tenantId);
        return tenant?.GetConnectionString();
    }

    private static string ExtractDatabaseNameFromConnectionString(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}