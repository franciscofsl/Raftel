namespace Raftel.Application.Abstractions.Multitenancy;

/// <summary>
/// Demonstration service that shows how tenant-specific connection strings can be used
/// for database operations when tenants have their own databases.
/// </summary>
public interface ITenantSpecificDatabaseService
{
    /// <summary>
    /// Gets information about the current tenant's database connection.
    /// This demonstrates that the system can resolve and use tenant-specific connection strings.
    /// </summary>
    /// <returns>Information about the database connection being used.</returns>
    Task<TenantDatabaseInfo> GetTenantDatabaseInfoAsync();
}

/// <summary>
/// Information about a tenant's database connection.
/// </summary>
public record TenantDatabaseInfo(
    Guid? TenantId,
    string ConnectionString,
    bool IsUsingTenantSpecificDatabase,
    string DatabaseName);