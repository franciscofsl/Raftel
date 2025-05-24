using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.ValueObjects;

namespace Raftel.Domain.Features.Tenants;

public interface ITenantsRepository : IRepository<Tenant, TenantId>
{
    Task<bool> CodeIsUniqueAsync(Code code, CancellationToken token);
} 