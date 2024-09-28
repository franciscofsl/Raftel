using FluentAssertions;
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
}