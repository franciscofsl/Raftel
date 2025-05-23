using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Domain.Features.Tenants;

public interface ITenantsRepository : IRepository<Tenant, TenantId>
{
    Task<bool> CodeIsUniqueAsync(string code, CancellationToken token);
} 