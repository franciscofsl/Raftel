using Raftel.Application.Commands;

namespace Raftel.Application.Features.Tenants.CreateTenant;

[RequiresPermission(TenantsPermissions.Management)]
public sealed record CreateTenantCommand(string Name, string Code, string Description, string ConnectionString = null) : ICommand;