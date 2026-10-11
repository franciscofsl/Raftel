using Raftel.Application.Abstractions.Concurrency;

namespace Raftel.Application.Features.Tenants.GetTenant;

public sealed class GetTenantResponse : IVersionedResult
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
} 