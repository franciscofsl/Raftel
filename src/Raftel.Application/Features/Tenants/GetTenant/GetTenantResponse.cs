namespace Raftel.Application.Features.Tenants.GetTenant;

public sealed class GetTenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
} 