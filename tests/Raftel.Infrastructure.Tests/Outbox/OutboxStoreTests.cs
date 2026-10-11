using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public abstract class OutboxStoreTestsBase : OutboxTestBase
{
    protected OutboxStoreTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureOptions(OutboxOptions options)
    {
        options.BatchSize = 2;
        options.MaxAttempts = 3;
        options.LockDuration = TimeSpan.FromMinutes(1);
    }

    private OutboxMessage NewMessage(int minutesAgo = 0, Guid? tenantId = null) =>
        OutboxMessage.Create("Demo.Event, Demo", "{}", Start.AddMinutes(-minutesAgo), tenantId, null);

    private async Task SeedAsync(params OutboxMessage[] messages)
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.AddRange(messages);
            await dbContext.SaveChangesAsync();
        });
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimAsync()
    {
        IReadOnlyList<OutboxMessage> claimed = null;
        await ExecuteScopedAsync(async sp =>
        {
            claimed = await sp.GetRequiredService<IOutboxStore>().ClaimAsync();
        });
        return claimed;
    }

    private async Task<OutboxMessage> ReloadAsync(OutboxMessage message)
    {
        OutboxMessage loaded = null;
        await ExecuteScopedAsync(async sp =>
        {
            loaded = await sp.GetRequiredService<TestingRaftelDbContext>().OutboxMessage
                .IgnoreQueryFilters().AsNoTracking().SingleAsync(m => m.Id == message.Id);
        });
        return loaded;
    }

    [Fact]
    public async Task Claim_ShouldReturnTheOldestFirst_UpToTheBatchSize_LeasingAndCountingTheAttempt()
    {
        var oldest = NewMessage(minutesAgo: 30);
        var middle = NewMessage(minutesAgo: 20);
        var newest = NewMessage(minutesAgo: 10);
        await SeedAsync(newest, oldest, middle);

        var claimed = await ClaimAsync();

        claimed.Select(m => m.Id).ShouldBe([oldest.Id, middle.Id]);
        claimed.ShouldAllBe(m => m.AttemptCount == 1);
        claimed.ShouldAllBe(m => m.LockedUntil == Start.AddMinutes(1));

        (await ReloadAsync(newest)).AttemptCount.ShouldBe(0);
        (await ReloadAsync(oldest)).AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Claim_ShouldNotReturnMessagesAlreadyLeased()
    {
        await SeedAsync(NewMessage(30), NewMessage(20));

        (await ClaimAsync()).Count.ShouldBe(2);
        (await ClaimAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Claim_ShouldSkipProcessed_FutureRetries_AndMessagesOutOfAttempts()
    {
        var processed = NewMessage(40);
        processed.MarkProcessed(Start);

        var waitingForRetry = NewMessage(30);
        waitingForRetry.MarkFailed(Start, "boom", Start.AddSeconds(10));

        var exhausted = NewMessage(20);
        exhausted.Claim(Start.AddSeconds(-1));
        exhausted.Claim(Start.AddSeconds(-1));
        exhausted.Claim(Start.AddSeconds(-1));

        var eligible = NewMessage(10);
        await SeedAsync(processed, waitingForRetry, exhausted, eligible);

        var claimed = await ClaimAsync();

        claimed.ShouldHaveSingleItem().Id.ShouldBe(eligible.Id);
    }

    [Fact]
    public async Task Claim_ShouldReturnAFailedMessage_OnceItsRetryIsDue()
    {
        var failed = NewMessage(30);
        failed.MarkFailed(Start, "boom", Start.AddSeconds(10));
        await SeedAsync(failed);

        (await ClaimAsync()).ShouldBeEmpty();

        Clock.Advance(TimeSpan.FromSeconds(10));

        (await ClaimAsync()).ShouldHaveSingleItem().Id.ShouldBe(failed.Id);
    }

    [Fact]
    public async Task Claim_ShouldReturnAMessageAgain_OnceItsLeaseExpired()
    {
        await SeedAsync(NewMessage(30));

        (await ClaimAsync()).ShouldHaveSingleItem();
        Clock.Advance(TimeSpan.FromSeconds(59));
        (await ClaimAsync()).ShouldBeEmpty();

        Clock.Advance(TimeSpan.FromSeconds(2));
        var reclaimed = (await ClaimAsync()).ShouldHaveSingleItem();

        reclaimed.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task Claim_ShouldIgnoreTheCurrentTenant()
    {
        await SeedAsync(NewMessage(30, Guid.NewGuid()), NewMessage(20, Guid.NewGuid()));
        IReadOnlyList<OutboxMessage> claimed = null;

        await ExecuteScopedAsync(async sp =>
        {
            using var tenant = sp.GetRequiredService<ICurrentTenant>().Change(Guid.NewGuid());
            claimed = await sp.GetRequiredService<IOutboxStore>().ClaimAsync();
        });

        claimed.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistTheOutcomeAppliedToClaimedMessages()
    {
        var message = NewMessage(30);
        await SeedAsync(message);

        await ExecuteScopedAsync(async sp =>
        {
            var store = sp.GetRequiredService<IOutboxStore>();
            var claimed = (await store.ClaimAsync()).Single();
            claimed.MarkProcessed(Clock.GetUtcNow());
            await store.SaveAsync();
        });

        var reloaded = await ReloadAsync(message);
        reloaded.ProcessedOn.ShouldBe(Start);
        reloaded.LockedUntil.ShouldBeNull();
        reloaded.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_ShouldDiscardTheOutcome_WhenTheLeaseWasLostToAnotherProcessor()
    {
        var message = NewMessage(30);
        await SeedAsync(message);
        var secondClaimSucceeded = false;

        await ExecuteScopedAsync(async firstProcessor =>
        {
            var firstStore = firstProcessor.GetRequiredService<IOutboxStore>();
            var firstClaim = (await firstStore.ClaimAsync()).Single();

            Clock.Advance(TimeSpan.FromMinutes(2));
            await ExecuteScopedAsync(async secondProcessor =>
            {
                var secondStore = secondProcessor.GetRequiredService<IOutboxStore>();
                var secondClaim = (await secondStore.ClaimAsync()).Single();
                secondClaimSucceeded = secondClaim.AttemptCount == 2;
            });

            firstClaim.MarkProcessed(Clock.GetUtcNow());
            (await firstStore.SaveAsync()).ShouldBeFalse();
        });

        secondClaimSucceeded.ShouldBeTrue();
        var reloaded = await ReloadAsync(message);
        reloaded.ProcessedOn.ShouldBeNull();
        reloaded.AttemptCount.ShouldBe(2);
        reloaded.LockedUntil.ShouldBe(Start.AddMinutes(3));
    }

    [Fact]
    public async Task GetDeadLetters_ShouldReturnOnlyUnprocessedMessagesOutOfAttempts()
    {
        var deadLetter = NewMessage(30);
        for (var i = 0; i < 3; i++)
        {
            deadLetter.Claim(Start);
        }

        deadLetter.MarkFailed(Start, "last error", Start);
        var pending = NewMessage(20);
        var processedAfterManyAttempts = NewMessage(10);
        for (var i = 0; i < 3; i++)
        {
            processedAfterManyAttempts.Claim(Start);
        }

        processedAfterManyAttempts.MarkProcessed(Start);
        await SeedAsync(deadLetter, pending, processedAfterManyAttempts);
        IReadOnlyList<OutboxMessage> deadLetters = null;

        await ExecuteScopedAsync(async sp =>
        {
            deadLetters = await sp.GetRequiredService<IOutboxStore>().GetDeadLettersAsync();
        });

        var found = deadLetters.ShouldHaveSingleItem();
        found.Id.ShouldBe(deadLetter.Id);
        found.AttemptCount.ShouldBe(3);
        found.Error.ShouldBe("last error");
    }
}

public abstract class OutboxStoreConcurrencyTestsBase : OutboxTestBase
{
    private const int MessageCount = 200;

    protected OutboxStoreConcurrencyTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureOptions(OutboxOptions options)
    {
        options.BatchSize = 7;
    }

    [Fact]
    public async Task TwoStoresClaimingConcurrently_ShouldNeverReceiveTheSameMessage()
    {
        var messages = Enumerable.Range(0, MessageCount)
            .Select(i => OutboxMessage.Create("Demo.Event, Demo", "{}", Start.AddSeconds(i), null, null))
            .ToList();

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.AddRange(messages);
            await dbContext.SaveChangesAsync();
        });

        var claimedBy = await Task.WhenAll(ClaimUntilEmptyAsync(), ClaimUntilEmptyAsync());

        var all = claimedBy.SelectMany(ids => ids).ToList();
        all.Count.ShouldBe(MessageCount);
        all.Distinct().Count().ShouldBe(MessageCount);
        claimedBy[0].ShouldNotBeEmpty();
        claimedBy[1].ShouldNotBeEmpty();
    }

    private async Task<List<Guid>> ClaimUntilEmptyAsync()
    {
        var ids = new List<Guid>();

        await Task.Yield();
        await ExecuteScopedAsync(async sp =>
        {
            var store = sp.GetRequiredService<IOutboxStore>();

            while (true)
            {
                var claimed = await store.ClaimAsync();
                if (claimed.Count == 0)
                {
                    return;
                }

                ids.AddRange(claimed.Select(m => (Guid)m.Id));
            }
        });

        return ids;
    }
}

public abstract class OutboxIdempotencyStoreTestsBase : OutboxTestBase
{
    protected OutboxIdempotencyStoreTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    private async Task<OutboxMessage> SeedMessageAsync()
    {
        var message = OutboxMessage.Create("Demo.Event, Demo", "{}", Start, null, null);
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.Add(message);
            await dbContext.SaveChangesAsync();
        });
        return message;
    }

    [Fact]
    public async Task HasCompleted_ShouldBeFalse_ThenTrueOnceMarked_PerHandler()
    {
        var message = await SeedMessageAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var store = sp.GetRequiredService<IIdempotencyStore>();

            (await store.HasCompletedAsync(message.Id, "HandlerA")).ShouldBeFalse();
            await store.MarkCompletedAsync(message.Id, "HandlerA");

            (await store.HasCompletedAsync(message.Id, "HandlerA")).ShouldBeTrue();
            (await store.HasCompletedAsync(message.Id, "HandlerB")).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task MarkCompleted_ShouldFail_ForADuplicateReceipt()
    {
        var message = await SeedMessageAsync();

        await ExecuteScopedAsync(async sp =>
        {
            await sp.GetRequiredService<IIdempotencyStore>().MarkCompletedAsync(message.Id, "HandlerA");
        });

        await ExecuteScopedAsync(async sp =>
        {
            await Should.ThrowAsync<DbUpdateException>(
                () => sp.GetRequiredService<IIdempotencyStore>().MarkCompletedAsync(message.Id, "HandlerA"));
        });
    }

    [Fact]
    public async Task MarkCompleted_ShouldNotSurvive_ARolledBackTransaction()
    {
        var message = await SeedMessageAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var transaction = await sp.GetRequiredService<IUnitOfWork>().BeginTransactionAsync();
            await sp.GetRequiredService<IIdempotencyStore>().MarkCompletedAsync(message.Id, "HandlerA");
            await transaction.RollbackAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            (await sp.GetRequiredService<IIdempotencyStore>().HasCompletedAsync(message.Id, "HandlerA"))
                .ShouldBeFalse();
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxStoreTests : OutboxStoreTestsBase
{
    public SqlServerOutboxStoreTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxStoreTests : OutboxStoreTestsBase
{
    public PostgreSqlOutboxStoreTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxStoreConcurrencyTests : OutboxStoreConcurrencyTestsBase
{
    public SqlServerOutboxStoreConcurrencyTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxStoreConcurrencyTests : OutboxStoreConcurrencyTestsBase
{
    public PostgreSqlOutboxStoreConcurrencyTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxIdempotencyStoreTests : OutboxIdempotencyStoreTestsBase
{
    public SqlServerOutboxIdempotencyStoreTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxIdempotencyStoreTests : OutboxIdempotencyStoreTestsBase
{
    public PostgreSqlOutboxIdempotencyStoreTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
