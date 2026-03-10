using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
    public Task<(IReadOnlyList<Pirate> Items, int TotalCount)> SearchPagedAsync(int page, int pageSize,
        string nameFilter = null, CancellationToken cancellationToken = default)
    {
        Expression<Func<Pirate, bool>> filter = !string.IsNullOrEmpty(nameFilter)
            ? p => ((string)p.Name).Contains(nameFilter)
            : null;

        return ListPagedAsync(page, pageSize, filter, cancellationToken);
    }
}