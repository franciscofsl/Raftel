using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Outbox;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public abstract class OutboxCleanupTestsBase : OutboxTestBase
{
    protected OutboxCleanupTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddLogging();
    }

    protected override void ConfigureOptions(OutboxOptions options)
    {
        options.RetentionPeriod = TimeSpan.FromDays(7);
        options.MaxAttempts = 2;
    }

    private static OutboxMessage Message(DateTimeOffset occurredOn) =>
        OutboxMessage.Create("Demo.Event, Demo", "{}", occurredOn, null, null);

    private async Task<(List<Guid> Messages, int Receipts)> SnapshotAsync()
    {
        var ids = new List<Guid>();
        var receipts = 0;
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            ids = (await dbContext.OutboxMessage.IgnoreQueryFilters().Select(m => m.Id).ToListAsync())
                .Select(id => (Guid)id).ToList();
            receipts = await dbContext.Set<OutboxHandlerReceipt>().CountAsync();
        });
        return (ids, receipts);
    }

    [Fact]
    public async Task Cleanup_ShouldDeleteOldProcessedMessagesWithTheirReceipts_AndKeepEverythingElse()
    {
        var oldProcessed = Message(Start.AddDays(-30));
        oldProcessed.MarkProcessed(Start.AddDays(-8));
        var recentProcessed = Message(Start.AddDays(-30));
        recentProcessed.MarkProcessed(Start.AddDays(-1));
        var oldPending = Message(Start.AddDays(-30));
        var oldDeadLetter = Message(Start.AddDays(-30));
        oldDeadLetter.Claim(Start.AddDays(-29));
        oldDeadLetter.Claim(Start.AddDays(-29));
        oldDeadLetter.MarkFailed(Start.AddDays(-29), "gave up", Start.AddDays(-29));

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.AddRange(oldProcessed, recentProcessed, oldPending, oldDeadLetter);
            await dbContext.SaveChangesAsync();

            var receipts = sp.GetRequiredService<IIdempotencyStore>();
            await receipts.MarkCompletedAsync(oldProcessed.Id, "HandlerA");
            await receipts.MarkCompletedAsync(oldProcessed.Id, "HandlerB");
            await receipts.MarkCompletedAsync(recentProcessed.Id, "HandlerA");
        });

        var deleted = await GetService<OutboxCleanupService>().RunOnceAsync(default);

        deleted.ShouldBe(1);
        var (messages, receiptCount) = await SnapshotAsync();
        messages.Count.ShouldBe(3);
        messages.ShouldNotContain(oldProcessed.Id);
        messages.ShouldContain(recentProcessed.Id);
        messages.ShouldContain(oldPending.Id);
        messages.ShouldContain(oldDeadLetter.Id);
        receiptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Cleanup_ShouldHonourTheRetentionBoundary_AsTheClockAdvances()
    {
        var message = Message(Start.AddDays(-1));
        message.MarkProcessed(Start);
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.Add(message);
            await dbContext.SaveChangesAsync();
        });

        Clock.Advance(TimeSpan.FromDays(7));
        (await GetService<OutboxCleanupService>().RunOnceAsync(default)).ShouldBe(0);

        Clock.Advance(TimeSpan.FromSeconds(1));
        (await GetService<OutboxCleanupService>().RunOnceAsync(default)).ShouldBe(1);
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxCleanupTests : OutboxCleanupTestsBase
{
    public SqlServerOutboxCleanupTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxCleanupTests : OutboxCleanupTestsBase
{
    public PostgreSqlOutboxCleanupTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
