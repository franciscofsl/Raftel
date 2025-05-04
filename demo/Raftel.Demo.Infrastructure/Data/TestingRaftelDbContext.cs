using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data.Configuration;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Demo.Infrastructure.Data;

public class TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options, IDataFilter dataFilter)
    : RaftelDbContext<TestingRaftelDbContext>(options, dataFilter)
{
    public DbSet<Pirate> Pirates { get; set; }

    public DbSet<Ship> Ships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestingRaftelDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}