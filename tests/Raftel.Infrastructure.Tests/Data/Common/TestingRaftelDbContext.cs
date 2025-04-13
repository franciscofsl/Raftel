using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Tests.Common.PiratesEntities;
using Raftel.Infrastructure.Tests.Data.Common.Configuration;

namespace Raftel.Infrastructure.Tests.Data.Common;

public class TestingRaftelDbContext : RaftelDbContext<TestingRaftelDbContext>
{
    public DbSet<Pirate> Pirates => Set<Pirate>();

    public TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new PirateConfiguration());
    }
}