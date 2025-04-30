using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;
using Raftel.Tests.Common.Domain;
using Raftel.Tests.Common.Infrastructure.Data.Configuration;

namespace Raftel.Tests.Common.Infrastructure.Data;

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