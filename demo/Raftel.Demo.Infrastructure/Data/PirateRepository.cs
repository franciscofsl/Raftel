using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
    public async Task<(IReadOnlyList<Pirate> Items, int TotalCount)> SearchPagedAsync(int page, int pageSize,
        string nameFilter = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Pirate>().AsQueryable();

        if (!string.IsNullOrEmpty(nameFilter))
        {
            query = query.Where(p => EF.Property<string>(p, "Name").Contains(nameFilter));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}