using Raftel.Application.Queries;

namespace Raftel.Application.Features.Tenants.GetAllTenants;

[RequiresPermission(TenantsPermissions.View)]
public sealed record GetAllTenantsQuery(int? Page, int? PageSize, string Sort)
    : IPagedQuery<GetAllTenantsResponse>;