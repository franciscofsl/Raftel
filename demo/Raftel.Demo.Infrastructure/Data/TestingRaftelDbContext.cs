using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data.Configuration;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Demo.Infrastructure.Data;

public class TestingRaftelDbContext
    : RaftelDbContext<TestingRaftelDbContext>
{
    public DbSet<Pirate> Pirates { get; set; }

    public DbSet<Ship> Ships { get; set; }

    public DbSet<DevilFruit> DevilFruits { get; set; }

    public TestingRaftelDbContext()
    {
    }

    public TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options)
        : base(options)
    {
    }
    
    public TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options, IDataFilter dataFilter)
        : base(options, dataFilter)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestingRaftelDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}