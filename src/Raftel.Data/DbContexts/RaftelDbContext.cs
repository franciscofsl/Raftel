using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Raftel.Core.Attributes;
using Raftel.Core.Auditing;
using Raftel.Core.Events;
using Raftel.Data.Outbox;

namespace Raftel.Data.DbContexts;

public class RaftelDbContext<TDbContext> : DbContext, IDbContext
    where TDbContext : RaftelDbContext<TDbContext>
{
    [ExcludeFromCodeCoverage]
    public RaftelDbContext()
    {
    }

    public RaftelDbContext(DbContextOptions<TDbContext> options, IDomainEventPublisher domainEventPublisher) :
        base(options)
    {
        DomainEventDomainEventPublisher = domainEventPublisher;
    }

    protected IDomainEventPublisher DomainEventDomainEventPublisher { get; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public DbSet<EntityChange> EntityChanges { get; set; }

    [ExcludeFromCodeCoverage]
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost,1434;Database=CookBook;User=sa;Password=SqlServer_Docker2023;MultipleActiveResultSets=true;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<AuditEventKind>();
        // modelBuilder.Entity<AuditEventKind>().HasNoKey();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaftelDbContext<>).Assembly);
    }
}