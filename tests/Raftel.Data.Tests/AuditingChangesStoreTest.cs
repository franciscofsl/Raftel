using System.Collections.ObjectModel;
using FluentAssertions;
using Raftel.Core.Attributes;
using Raftel.Data.DbContexts;
using Raftel.Data.DbContexts.Auditing;
using Raftel.Data.Tests.DbContext;
using Raftel.Data.Tests.Types.Models;

namespace Raftel.Data.Tests;

public class AuditingChangesStoreTest(TestingDbFixture fixture) : DataTestBase(fixture)
{
    [Fact]
    public void CreateLog_WithoutChanges_ShouldCreateEmptyLog()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        log.Should().NotBeNull();
        log.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public async Task CreateLog_ShouldCreateLog_WithAuditedEntries()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        await dbContext.AddAsync(SampleAggregate.Create());

        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        log.IsEmpty().Should().BeFalse();
    }

    [Fact]
    public async Task CreateLog_ShouldNotCreateLog_WithNotAuditableEntries()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        await dbContext.AddAsync(SampleNotAuditedAggregate.Create());

        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        log.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public async Task EntityChange_WithChange_ShouldCreateCreateEntityChange_WithOldValueEmpty()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var aggregate = SampleAggregate.Create();
        aggregate.StringValue = "Monkey D. Luffy";
        aggregate.IntegerValue = 123;
        aggregate.Processed = true;

        await dbContext.AddAsync(aggregate);

        var log = changesStore.CreateLog(dbContext.ChangeTracker);
        log.Should().ContainSingle();

        var entityChange = log.First();
        entityChange.OccurredOn.Should().BeBefore(DateTime.UtcNow);
        entityChange.EntityId.Should().Be(aggregate.Id.ToString());
        entityChange.Kind.Should().Be(AuditEventKind.Created);
        entityChange.Properties.Should().HaveCount(3);

        foreach (var property in entityChange.Properties)
        {
            var entityProperty = aggregate.GetType().GetProperty(property.Name);

            property.OldValue.Should().BeNullOrEmpty();
            property.NewValue.Should().Be(entityProperty.GetValue(aggregate)?.ToString());
        }
    }
}