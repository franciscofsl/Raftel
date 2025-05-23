using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.ValueObjects;

namespace Raftel.Infrastructure.Data.Repositories.Tenants;

internal sealed class TenantsRepository<TDbContext>(TDbContext dbContext)
    : EfRepository<TDbContext, Tenant, TenantId>(dbContext), ITenantsRepository
    where TDbContext : RaftelDbContext<TDbContext>
{
    public async Task<bool> CodeIsUniqueAsync(Code code, CancellationToken token)
    {
        return !await dbContext.Set<Tenant>()
            .AnyAsync(x => x.Code == code, token);
    }
} 