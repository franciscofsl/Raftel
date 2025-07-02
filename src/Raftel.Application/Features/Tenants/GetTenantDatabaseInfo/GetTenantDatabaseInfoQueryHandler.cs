using Raftel.Application.Queries;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Tenants.GetTenantDatabaseInfo;

/// <summary>
/// Query to get information about the current tenant's database connection.
/// This demonstrates how the system resolves and uses tenant-specific connection strings.
/// </summary>
/// <param name="IncludeConnectionString">Whether to include the connection string in the response (for debugging purposes).</param>
public sealed record GetTenantDatabaseInfoQuery(
    bool IncludeConnectionString = false
) : IQuery<TenantDatabaseInfoResponse>;

/// <summary>
/// Response containing information about the tenant's database connection.
/// </summary>
public sealed record TenantDatabaseInfoResponse(
    Guid? TenantId,
    string? ConnectionString,
    bool IsUsingTenantSpecificDatabase,
    string DatabaseName,
    string Message);

/// <summary>
/// Handler that demonstrates how tenant-specific database connections are resolved and used.
/// </summary>
internal sealed class GetTenantDatabaseInfoQueryHandler : IQueryHandler<GetTenantDatabaseInfoQuery, TenantDatabaseInfoResponse>
{
    private readonly ITenantSpecificDatabaseService _tenantDatabaseService;

    public GetTenantDatabaseInfoQueryHandler(ITenantSpecificDatabaseService tenantDatabaseService)
    {
        _tenantDatabaseService = tenantDatabaseService;
    }

    public async Task<Result<TenantDatabaseInfoResponse>> HandleAsync(GetTenantDatabaseInfoQuery request, CancellationToken token = default)
    {
        var databaseInfo = await _tenantDatabaseService.GetTenantDatabaseInfoAsync();

        var message = databaseInfo.IsUsingTenantSpecificDatabase
            ? $"Using tenant-specific database: {databaseInfo.DatabaseName}"
            : $"Using default shared database: {databaseInfo.DatabaseName}";

        var response = new TenantDatabaseInfoResponse(
            databaseInfo.TenantId,
            request.IncludeConnectionString ? databaseInfo.ConnectionString : null,
            databaseInfo.IsUsingTenantSpecificDatabase,
            databaseInfo.DatabaseName,
            message);

        return Result.Success(response);
    }
}