using Raftel.Application.Queries;

namespace Raftel.Application.Features.Tenants.GetTenant;

[RequiresPermission(TenantsPermissions.View)]
public sealed record GetTenantQuery(Guid Id) : IQuery<GetTenantResponse>; 