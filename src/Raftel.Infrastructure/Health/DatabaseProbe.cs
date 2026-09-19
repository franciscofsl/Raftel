using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Health;

public sealed class DatabaseProbe<TDbContext>(TDbContext dbContext) : IDatabaseProbe
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return dbContext.Database.CanConnectAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken)
    {
        var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending.ToArray();
    }
}
