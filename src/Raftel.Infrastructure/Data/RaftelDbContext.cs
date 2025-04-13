using Microsoft.EntityFrameworkCore;
using Raftel.Application;

namespace Raftel.Infrastructure.Data;

public abstract class RaftelDbContext<TDbContext> : DbContext, IUnitOfWork where TDbContext : RaftelDbContext<TDbContext>
{
    protected RaftelDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}