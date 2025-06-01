using Raftel.Application.Queries;

namespace Raftel.Application.Features.Tenants.GetCurrentTenant;

[RequiresPermission(TenantsPermissions.View)]
public sealed record GetCurrentTenantQuery() : IQuery<GetCurrentTenantResponse>;