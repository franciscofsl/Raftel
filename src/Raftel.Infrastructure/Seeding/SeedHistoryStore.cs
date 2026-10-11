using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Seeding;

internal sealed class SeedHistoryStore<TDbContext>(TDbContext dbContext, TimeProvider timeProvider) : ISeedHistoryStore
    where TDbContext : RaftelDbContext<TDbContext>
{
    public async Task<int?> GetAppliedVersionAsync(string name, Guid? tenantId, CancellationToken cancellationToken)
    {
        var entry = await FindAsync(name, tenantId, cancellationToken);
        return entry?.Version;
    }

    public async Task RecordAsync(string name, Guid? tenantId, int version, CancellationToken cancellationToken)
    {
        var appliedOn = timeProvider.GetUtcNow().UtcDateTime;
        var entry = await FindAsync(name, tenantId, cancellationToken);

        if (entry is null)
        {
            await dbContext.Set<SeedHistory>().AddAsync(new SeedHistory(name, tenantId, version, appliedOn), cancellationToken);
            return;
        }

        entry.Reapply(version, appliedOn);
    }

    private Task<SeedHistory?> FindAsync(string name, Guid? tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Set<SeedHistory>()
            .FirstOrDefaultAsync(h => h.Name == name && h.TenantId == tenantId, cancellationToken);
    }
}
