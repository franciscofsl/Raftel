using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data.Configuration;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

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