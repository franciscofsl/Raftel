using Microsoft.EntityFrameworkCore;
using Raftel.Core.Events;
using Raftel.Data.DbContexts;

namespace Raftel.Data.Tests.Types.Models;

public class TestingDbContext : RaftelDbContext<TestingDbContext>
{
    public TestingDbContext()
    {
    }

    public TestingDbContext(DbContextOptions<TestingDbContext> options, IDomainEventPublisher domainEventPublisher)
        : base(options, domainEventPublisher)
    {
    }

    public DbSet<SampleAggregate> SampleModels { get; set; }
    public DbSet<SampleNotAuditedAggregate> SampleNotAuditedAggregates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SampleAggregate>().HasKey(_ => _.Id);
        modelBuilder.Entity<SampleAggregate>().Property(x => x.Id)
            .HasConversion(x => x.Value, _ => (SampleId)_)
            .IsRequired();
        
        modelBuilder.Entity<SampleNotAuditedAggregate>().HasKey(_ => _.Id);
        modelBuilder.Entity<SampleNotAuditedAggregate>().Property(x => x.Id)
            .HasConversion(x => x.Value, _ => (SampleId)_)
            .IsRequired();
    }
}