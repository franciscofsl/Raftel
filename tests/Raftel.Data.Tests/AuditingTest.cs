using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Raftel.Core.Attributes;
using Raftel.Core.Auditing;
using Raftel.Data.DbContexts;
using Raftel.Data.DbContexts.Auditing;
using Raftel.Data.Tests.DbContext;
using Raftel.Data.Tests.Types.Models;

namespace Raftel.Data.Tests;

[Collection("SequentialDbContextTests")]
public class AuditingTest(TestingDbFixture fixture) : DataTestBase(fixture)
{
    [Fact]
    public void CreateLog_WithoutChanges_ShouldCreateEmptyLog()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        log.Should().NotBeNull();
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

        var notAuditableEntity = SampleNotAuditedAggregate.Create();
        await dbContext.AddAsync(notAuditableEntity);

        var log = changesStore.CreateLog(dbContext.ChangeTracker);
        log.Should().NotContain(_ => _.EntityId == notAuditableEntity.Id.ToString());
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

        var entityChange = log.First(_ => _.EntityId == aggregate.Id.ToString());
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

    [Fact]
    public async Task EntityChange_WithChange_ShouldCreateUpdatedEntityChange_WithOnlyModifiedProperties()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var oldAggregate = SampleAggregate.Create();
        oldAggregate.StringValue = "Monkey D. Luffy";
        oldAggregate.IntegerValue = 123;
        oldAggregate.Processed = true;
        await dbContext.AddAsync(oldAggregate);
        await dbContext.SaveChangesAsync();

        var originalValues = await dbContext.Set<SampleAggregate>().AsNoTracking()
            .FirstOrDefaultAsync(_ => _.Id == oldAggregate.Id);

        var updatedAggregate = await dbContext.Set<SampleAggregate>().FirstOrDefaultAsync(_ => _.Id == oldAggregate.Id);
        updatedAggregate.StringValue = "Tony Tony Chopper";
        var updateLog = changesStore.CreateLog(dbContext.ChangeTracker);

        var updateEvent = updateLog.First();
        updateEvent.OccurredOn.Should().BeBefore(DateTime.UtcNow);
        updateEvent.EntityId.Should().Be(oldAggregate.Id.ToString());
        updateEvent.Kind.Should().Be(AuditEventKind.Updated);
        updateEvent.Properties.Should().HaveCount(1);

        foreach (var property in updateEvent.Properties)
        {
            var entityProperty = typeof(SampleAggregate).GetProperty(property.Name);
            property.NewValue.Should().Be(entityProperty.GetValue(updatedAggregate)?.ToString());
            property.OldValue.Should().Be(entityProperty.GetValue(originalValues)?.ToString());
        }
    }

    [Fact]
    public async Task EntityChange_WithChange_ShouldCreateUpdatedEntityChange()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var oldAggregate = SampleAggregate.Create();
        oldAggregate.StringValue = "Monkey D. Luffy";
        oldAggregate.IntegerValue = 123;
        oldAggregate.Processed = true;
        await dbContext.AddAsync(oldAggregate);
        await dbContext.SaveChangesAsync();

        var originalValues = await dbContext.Set<SampleAggregate>().AsNoTracking()
            .FirstOrDefaultAsync(_ => _.Id == oldAggregate.Id);

        var updatedAggregate = await dbContext.Set<SampleAggregate>().FirstOrDefaultAsync(_ => _.Id == oldAggregate.Id);
        updatedAggregate.StringValue = "Roronoa Zoro";
        updatedAggregate.IntegerValue = 321;
        updatedAggregate.Processed = false;
        var updateLog = changesStore.CreateLog(dbContext.ChangeTracker);

        var updateEvent = updateLog.First();
        updateEvent.OccurredOn.Should().BeBefore(DateTime.UtcNow);
        updateEvent.EntityId.Should().Be(oldAggregate.Id.ToString());
        updateEvent.Kind.Should().Be(AuditEventKind.Updated);
        updateEvent.Properties.Should().HaveCount(3);

        foreach (var property in updateEvent.Properties)
        {
            var entityProperty = typeof(SampleAggregate).GetProperty(property.Name);
            property.NewValue.Should().Be(entityProperty.GetValue(updatedAggregate)?.ToString());
            property.OldValue.Should().Be(entityProperty.GetValue(originalValues)?.ToString());
        }
    }

    [Fact]
    public async Task EntityChange_WithChange_ShouldCreateDeleteChange_WithoutProperties()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var changesStore = GetRequiredService<AuditChangesStore>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var entity = SampleAggregate.Create();
        entity.StringValue = "Monkey D. Luffy";
        entity.IntegerValue = 123;
        entity.Processed = true;
        await dbContext.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        dbContext.Remove(entity);
        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        var deleteEvent = log.First();
        deleteEvent.Kind.Should().Be(AuditEventKind.Deleted);
        deleteEvent.Properties.Should().BeEmpty();
    }

    [Fact]
    public async Task EntityChangesLog_ShouldSaveLog_OnEntitySave()
    {
        var dbContextFactory = GetRequiredService<IDbContextFactory>();
        var entityChangesReader = GetRequiredService<IEntityChangesReader>();
        var dbContext = dbContextFactory.Create<TestingDbContext>();

        var entity = SampleAggregate.Create();
        entity.StringValue = "Monkey D. Luffy";
        entity.IntegerValue = 123;
        entity.Processed = true;
        await dbContext.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        var log = await entityChangesReader.ForEntityAsync(entity.Id.ToString());

        log.IsEmpty().Should().BeFalse();
    }
}