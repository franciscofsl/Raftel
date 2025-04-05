using Microsoft.EntityFrameworkCore;
using Raftel.Data.Interceptors;

namespace Raftel.Data.DbContexts;

public abstract class RaftelDbContext<TDbContext> : DbContext where TDbContext : RaftelDbContext<TDbContext>
{
    protected RaftelDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}