namespace Raftel.Application.Features.Tenants.GetCurrentTenant;

public sealed class GetCurrentTenantResponse
{
    public Guid? TenantId { get; set; }
    public bool IsMultiTenant { get; set; }
} 