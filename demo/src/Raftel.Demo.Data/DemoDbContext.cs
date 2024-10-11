using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Raftel.Core.Events;
using Raftel.Data.DbContexts;
using Raftel.Demo.Core.Samples;

namespace Raftel.Demo.Data;

public class DemoDbContext : RaftelDbContext<DemoDbContext>
{
    public DemoDbContext()
    {
    }

    public DemoDbContext(DbContextOptions<DemoDbContext> options, IDomainEventPublisher domainEventPublisher)
        : base(options, domainEventPublisher)
    {
    }

    public DbSet<Sample> Samples { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}