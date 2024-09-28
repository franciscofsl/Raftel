using FluentAssertions;
using Raftel.Data.DbContexts.Auditing;
using Raftel.Data.Tests.DbContext;
using Raftel.Data.Tests.Types.Models;

namespace Raftel.Data.Tests;

public class AuditingChangesStoreTest(TestingDbFixture fixture) : DataTestBase(fixture)
{
    [Fact]
    public void CreateLog_WithoutChanges_ShouldCreateEmptyLog()
    {
        var dbContext = GetRequiredService<TestingDbContext>();

        var changesStore = GetRequiredService<AuditChangesStore>();

        var log = changesStore.CreateLog(dbContext.ChangeTracker);

        log.Should().NotBeNull();
        log.IsEmpty().Should().BeTrue();
    }
}