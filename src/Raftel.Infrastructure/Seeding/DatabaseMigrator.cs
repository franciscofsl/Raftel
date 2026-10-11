using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Seeding;

public sealed class DatabaseMigrator<TDbContext>(TDbContext dbContext) : IDatabaseMigrator
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task MigrateAsync(CancellationToken cancellationToken)
    {
        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}
