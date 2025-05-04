using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data.Configuration;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class TestingRaftelDbContext : RaftelDbContext<TestingRaftelDbContext>
{
    public DbSet<Pirate> Pirates { get; set; }

    public DbSet<Ship> Ships { get; set; }


    public TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options,
        IDataFilter dataFilter)
        : base(options, dataFilter)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestingRaftelDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}