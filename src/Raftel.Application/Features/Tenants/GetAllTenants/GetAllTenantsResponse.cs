namespace Raftel.Application.Features.Tenants.GetAllTenants;

public sealed class GetAllTenantsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
} 