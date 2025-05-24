using Raftel.Application.Commands;

namespace Raftel.Application.Features.Tenants.CreateTenant;

public sealed record CreateTenantCommand(string Name, string Code, string Description) : ICommand; 