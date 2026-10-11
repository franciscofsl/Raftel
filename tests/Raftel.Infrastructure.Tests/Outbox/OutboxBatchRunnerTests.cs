using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Outbox;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

/// <summary>Shared arrangement and queries for the runner tests. Holds no facts, so subclasses do not inherit any.</summary>
public abstract class OutboxRunnerTestSupport : OutboxTestBase
{
    protected OutboxRunnerTestSupport(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureOptions(OutboxOptions options)
    {
        options.BatchSize = 10;
        options.MaxAttempts = 3;
        options.BaseRetryDelay = TimeSpan.FromSeconds(10);
        options.MaxRetryDelay = TimeSpan.FromSeconds(30);
        options.LockDuration = TimeSpan.FromMinutes(1);
    }

    /// <summary>The handlers a test wants running, besides the demo's own.</summary>
    protected virtual void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
    }

    protected sealed override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddLogging(logging => logging.AddProvider(Logs));
        ConfigureHandlers(services);
    }

    protected CapturingLoggerProvider Logs { get; } = new();

    protected Task RunCycleAsync(CancellationToken cancellationToken = default) =>
        GetService<OutboxBatchRunner>().RunOnceAsync(cancellationToken);

    protected async Task<PirateCrownedKing> CrownPirateAsync(string name = "Luffy", Guid? tenantId = null,
        string correlationId = null)
    {
        PirateCrownedKing raised = null;

        await ExecuteScopedAsync(async sp =>
        {
            using var tenant = sp.GetRequiredService<ICurrentTenant>().Change(tenantId);
            using var correlation = sp.GetRequiredService<Raftel.Application.Abstractions.ICorrelationScope>()
                .Begin(correlationId);

            var pirate = Pirate.Normal(name, 100_000_000);
            pirate.FoundOnePiece();
            raised = pirate.DomainEvents.OfType<PirateCrownedKing>().Single();
            await sp.GetRequiredService<IPirateRepository>().AddAsync(pirate);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        return raised;
    }

    protected async Task<List<OutboxMessage>> MessagesAsync()
    {
        List<OutboxMessage> messages = null;
        await ExecuteScopedAsync(async sp =>
        {
            messages = await sp.GetRequiredService<TestingRaftelDbContext>().OutboxMessage
                .IgnoreQueryFilters().AsNoTracking().OrderBy(m => m.OccurredOn).ToListAsync();
        });
        return messages;
    }

    protected async Task<OutboxMessage> SingleMessageAsync() => (await MessagesAsync()).ShouldHaveSingleItem();

    private async Task<int> CountAsync<T>() where T : class
    {
        var count = 0;
        await ExecuteScopedAsync(async sp =>
        {
            count = await sp.GetRequiredService<TestingRaftelDbContext>().Set<T>().IgnoreQueryFilters().CountAsync();
        });
        return count;
    }

    protected Task<int> ReceiptCountAsync() => CountAsync<OutboxHandlerReceipt>();

    protected Task<int> PirateCountAsync(string name = null)
    {
        var count = 0;
        return ExecuteScopedAsync(async sp =>
        {
            var query = sp.GetRequiredService<TestingRaftelDbContext>().Set<Pirate>().IgnoreQueryFilters();
            count = name is null ? await query.CountAsync() : await query.CountAsync(p => p.Name == name);
        }).ContinueWith(_ => count);
    }
}

public abstract class OutboxBatchRunnerTestsBase : OutboxRunnerTestSupport
{
    protected OutboxBatchRunnerTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    // ---- 8.1 delivery -------------------------------------------------------------------------------------

    [Fact]
    public async Task Cycle_ShouldDeliverTheEventOnce_AndMarkTheMessageProcessed()
    {
        var raised = await CrownPirateAsync();

        await RunCycleAsync();

        var call = Log.CallsOf(nameof(RecordingHandler)).ShouldHaveSingleItem();
        call.Event.PirateId.ShouldBe(raised.PirateId);
        call.Event.OccurredOn.ShouldBe(raised.OccurredOn);

        var message = await SingleMessageAsync();
        message.ProcessedOn.ShouldBe(Start);
        message.Error.ShouldBeNull();
        message.LockedUntil.ShouldBeNull();
    }

    [Fact]
    public async Task Cycle_ShouldNotRedeliverAProcessedMessage()
    {
        await CrownPirateAsync();

        await RunCycleAsync();
        await RunCycleAsync();

        Log.CallsOf(nameof(RecordingHandler)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Cycle_ShouldReportHowManyMessagesItClaimed()
    {
        await CrownPirateAsync("Luffy");
        await CrownPirateAsync("Zoro");

        (await GetService<OutboxBatchRunner>().RunOnceAsync(default)).ShouldBe(2);
        (await GetService<OutboxBatchRunner>().RunOnceAsync(default)).ShouldBe(0);
    }

    // ---- 8.2 retry and dead letter ------------------------------------------------------------------------

    [Fact]
    public async Task FailedDelivery_ShouldRecordTheError_AndWaitForTheBaseDelayBeforeRetrying()
    {
        Log.FailWhen = _ => true;
        await CrownPirateAsync();

        await RunCycleAsync();

        var failed = await SingleMessageAsync();
        failed.ProcessedOn.ShouldBeNull();
        failed.AttemptCount.ShouldBe(1);
        failed.Error.ShouldContain("selective failure");
        failed.NextAttemptOn.ShouldBe(Start.AddSeconds(10));

        Clock.Advance(TimeSpan.FromSeconds(9));
        await RunCycleAsync();
        Log.CallsOf(nameof(RecordingHandler)).Count.ShouldBe(1);

        Clock.Advance(TimeSpan.FromSeconds(1));
        await RunCycleAsync();
        Log.CallsOf(nameof(RecordingHandler)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task FailedDelivery_ShouldDoubleTheDelay_AndCapItAtTheMaximum()
    {
        Log.FailWhen = _ => true;
        await CrownPirateAsync();

        await RunCycleAsync();
        (await SingleMessageAsync()).NextAttemptOn.ShouldBe(Start.AddSeconds(10));

        Clock.Advance(TimeSpan.FromSeconds(10));
        await RunCycleAsync();
        (await SingleMessageAsync()).NextAttemptOn.ShouldBe(Start.AddSeconds(10 + 20));

        Clock.Advance(TimeSpan.FromSeconds(20));
        await RunCycleAsync();
        (await SingleMessageAsync()).NextAttemptOn.ShouldBe(Start.AddSeconds(30 + 30));
    }

    [Fact]
    public async Task RetryThatSucceeds_ShouldMarkTheMessageProcessed()
    {
        Log.FailWhen = _ => true;
        await CrownPirateAsync();
        await RunCycleAsync();

        Log.FailWhen = _ => false;
        Clock.Advance(TimeSpan.FromSeconds(10));
        await RunCycleAsync();

        var message = await SingleMessageAsync();
        message.ProcessedOn.ShouldNotBeNull();
        message.Error.ShouldBeNull();
        message.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task ExhaustedMessage_ShouldBecomeADeadLetter_AndNeverBeDeliveredAgain()
    {
        Log.FailWhen = _ => true;
        await CrownPirateAsync();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await RunCycleAsync();
            Clock.Advance(TimeSpan.FromMinutes(5));
        }

        await RunCycleAsync();
        Log.CallsOf(nameof(RecordingHandler)).Count.ShouldBe(3);

        IReadOnlyList<OutboxMessage> deadLetters = null;
        await ExecuteScopedAsync(async sp =>
        {
            deadLetters = await sp.GetRequiredService<IOutboxStore>().GetDeadLettersAsync();
        });

        var deadLetter = deadLetters.ShouldHaveSingleItem();
        deadLetter.AttemptCount.ShouldBe(3);
        deadLetter.Error.ShouldContain("selective failure");
    }

    [Fact]
    public async Task OneFailingMessage_ShouldNotStopTheRestOfTheBatch()
    {
        var bad = await CrownPirateAsync("Bad");
        var good = await CrownPirateAsync("Good");
        Log.FailWhen = e => e.PirateId == bad.PirateId;

        await RunCycleAsync();

        var messages = await MessagesAsync();
        messages.Count.ShouldBe(2);
        messages.Count(m => m.ProcessedOn is not null).ShouldBe(1);
        Log.CallsOf(nameof(RecordingHandler)).Select(c => c.Event.PirateId).ShouldContain(good.PirateId);
    }

    // ---- 8.5 unreadable messages and follow-up events -----------------------------------------------------

    [Fact]
    public async Task UnknownEventType_ShouldFailTheMessage_WithoutRunningAnyHandler_AndKeepGoing()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.Add(OutboxMessage.Create("Does.Not.Exist, Nowhere", "{}", Start.AddMinutes(-5), null, null));
            await dbContext.SaveChangesAsync();
        });
        await CrownPirateAsync();

        await RunCycleAsync();

        var messages = await MessagesAsync();
        var unknown = messages.Single(m => m.Type.StartsWith("Does.Not.Exist"));
        unknown.ProcessedOn.ShouldBeNull();
        unknown.AttemptCount.ShouldBe(1);
        unknown.Error.ShouldContain("Does.Not.Exist");
        messages.Single(m => m != unknown).ProcessedOn.ShouldNotBeNull();
        Log.CallsOf(nameof(RecordingHandler)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task MalformedContent_ShouldFailTheMessage_WithAnError()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.Add(OutboxMessage.Create(
                "Raftel.Demo.Domain.Pirates.Events.PirateCrownedKing, Raftel.Demo.Domain", "not json", Start, null, null));
            await dbContext.SaveChangesAsync();
        });

        await RunCycleAsync();

        var message = await SingleMessageAsync();
        message.ProcessedOn.ShouldBeNull();
        message.Error.ShouldNotBeNullOrWhiteSpace();
        Log.Calls.ShouldBeEmpty();
    }
}

public abstract class OutboxIdempotencyDeliveryTestsBase : OutboxRunnerTestSupport
{
    protected OutboxIdempotencyDeliveryTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, SecondHandler>();
    }

    [Fact]
    public async Task Retry_ShouldOnlyRunTheHandlersThatHaveNotCompleted()
    {
        Log.SecondHandlerFails = true;
        await CrownPirateAsync();

        await RunCycleAsync();
        Log.CallsOf(nameof(RecordingHandler)).Count.ShouldBe(1);
        Log.CallsOf(nameof(SecondHandler)).Count.ShouldBe(1);

        Log.SecondHandlerFails = false;
        Clock.Advance(TimeSpan.FromSeconds(10));
        await RunCycleAsync();

        Log.CallsOf(nameof(RecordingHandler)).Count.ShouldBe(1);
        Log.CallsOf(nameof(SecondHandler)).Count.ShouldBe(2);
        (await SingleMessageAsync()).ProcessedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task Redelivery_AfterEveryHandlerCompleted_ShouldRunNoHandlerAndMarkTheMessageProcessed()
    {
        await CrownPirateAsync();
        await RunCycleAsync();
        var runsBefore = Log.Calls.Count;

        await ExecuteScopedAsync(async sp =>
        {
            // Simulates a crash after the handlers committed but before the message was marked processed.
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            await dbContext.OutboxMessage.IgnoreQueryFilters().ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.ProcessedOn, (DateTimeOffset?)null));
        });
        (await SingleMessageAsync()).ProcessedOn.ShouldBeNull();

        await RunCycleAsync();

        Log.Calls.Count.ShouldBe(runsBefore);
        (await SingleMessageAsync()).ProcessedOn.ShouldNotBeNull();
    }
}

public abstract class OutboxAtomicHandlerTestsBase : OutboxRunnerTestSupport
{
    protected OutboxAtomicHandlerTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, WriteThenThrowHandler>();
    }

    [Fact]
    public async Task HandlerThatWritesThenFails_ShouldLeaveNeitherItsWritesNorAReceipt()
    {
        await CrownPirateAsync("Luffy");
        var piratesBefore = await PirateCountAsync();

        await RunCycleAsync();

        (await PirateCountAsync()).ShouldBe(piratesBefore);
        (await PirateCountAsync("Ghost")).ShouldBe(0);
        // The demo's own handler completed first and is the only receipt.
        (await ReceiptCountAsync()).ShouldBe(1);
        (await SingleMessageAsync()).Error.ShouldContain("failed after writing");
    }
}

public abstract class OutboxAmbientContextTestsBase : OutboxRunnerTestSupport
{
    protected OutboxAmbientContextTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, TenantWritingHandler>();
    }

    [Fact]
    public async Task Handler_ShouldRunUnderTheOriginatingTenantAndCorrelation()
    {
        var tenantId = Guid.NewGuid();
        await CrownPirateAsync(tenantId: tenantId, correlationId: "corr-77");

        await RunCycleAsync();

        var call = Log.CallsOf(nameof(RecordingHandler)).ShouldHaveSingleItem();
        call.Tenant.ShouldBe(tenantId);
        call.CorrelationId.ShouldBe("corr-77");
    }

    [Fact]
    public async Task HandlerWrites_ShouldBelongToTheOriginatingTenant()
    {
        var tenantId = Guid.NewGuid();
        await CrownPirateAsync(tenantId: tenantId);

        await RunCycleAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var written = await dbContext.Set<Pirate>().IgnoreQueryFilters().SingleAsync(p => p.Name == "FromHandler");

            dbContext.Entry(written).Property<Guid?>(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe(tenantId);
        });
    }

    [Fact]
    public async Task MessageCapturedWithoutATenant_ShouldRunWithNoCurrentTenant()
    {
        await CrownPirateAsync(tenantId: null);

        await RunCycleAsync();

        Log.CallsOf(nameof(RecordingHandler)).ShouldHaveSingleItem().Tenant.ShouldBeNull();
    }

    [Fact]
    public async Task ConsecutiveMessagesOfDifferentTenants_ShouldNotLeakAndLeaveNoTenantBehind()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await CrownPirateAsync("A", tenantA);
        Clock.Advance(TimeSpan.FromSeconds(1));
        await CrownPirateAsync("B", tenantB);

        await RunCycleAsync();

        Log.CallsOf(nameof(RecordingHandler)).Select(c => c.Tenant).ShouldBe([tenantA, tenantB], ignoreOrder: true);
        await ExecuteScopedAsync(sp =>
        {
            sp.GetRequiredService<ICurrentTenant>().Id.ShouldBeNull();
            return Task.CompletedTask;
        });
    }
}

public abstract class OutboxFollowUpTestsBase : OutboxRunnerTestSupport
{
    protected OutboxFollowUpTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, FollowUpHandler>();
    }

    [Fact]
    public async Task EventRaisedByAHandler_ShouldBeStoredAndDeliveredInALaterCycle()
    {
        Log.FollowUpsLeft = 1;
        await CrownPirateAsync();

        await RunCycleAsync();
        var afterFirst = await MessagesAsync();
        afterFirst.Count.ShouldBe(2);
        afterFirst.Count(m => m.ProcessedOn is not null).ShouldBe(1);

        await RunCycleAsync();
        (await MessagesAsync()).ShouldAllBe(m => m.ProcessedOn != null);
    }
}

public abstract class OutboxShutdownTestsBase : OutboxRunnerTestSupport
{
    protected OutboxShutdownTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureHandlers(IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, BlockingHandler>();
    }

    [Fact]
    public async Task Shutdown_ShouldLeaveTheInterruptedMessageUnprocessed_AndReleaseTheRestOfTheBatch()
    {
        await CrownPirateAsync("First");
        Clock.Advance(TimeSpan.FromSeconds(1));
        await CrownPirateAsync("Second");

        await RunCycleAsync(Log.Shutdown.Token);

        var messages = await MessagesAsync();
        messages.ShouldAllBe(m => m.ProcessedOn == null);
        messages.ShouldAllBe(m => m.LockedUntil == null);
        messages.ShouldAllBe(m => m.AttemptCount == 0);
    }
}

public abstract class OutboxLoggingTestsBase : OutboxRunnerTestSupport
{
    protected OutboxLoggingTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureOptions(OutboxOptions options)
    {
        options.MaxAttempts = 2;
    }

    private IEnumerable<LogEntry> OutboxEntries =>
        Logs.Entries.Where(entry => entry.State.ContainsKey("OutboxMessageId"));

    [Fact]
    public async Task SuccessfulDelivery_ShouldBeLoggedWithItsIdentifyingFields()
    {
        var tenantId = Guid.NewGuid();
        await CrownPirateAsync(tenantId: tenantId, correlationId: "corr-log");

        await RunCycleAsync();

        var entry = OutboxEntries.ShouldHaveSingleItem();
        var message = await SingleMessageAsync();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.State["OutboxMessageId"].ToString().ShouldBe(message.Id.ToString());
        entry.State["EventType"].ShouldBe(message.Type);
        entry.State["Attempt"].ShouldBe(1);
        entry.State["Outcome"].ShouldBe("Delivered");
        entry.State["TenantId"].ShouldBe(tenantId);
        entry.State["CorrelationId"].ShouldBe("corr-log");
    }

    [Fact]
    public async Task MessageBecomingADeadLetter_ShouldBeLoggedAtErrorLevel_AfterAWarningForTheFirstFailure()
    {
        Log.FailWhen = _ => true;
        await CrownPirateAsync();

        await RunCycleAsync();
        Clock.Advance(TimeSpan.FromMinutes(5));
        await RunCycleAsync();

        var entries = OutboxEntries.ToList();
        entries.Select(e => e.Level).ShouldBe([LogLevel.Warning, LogLevel.Error]);
        var deadLetter = entries[1];
        deadLetter.State["Outcome"].ShouldBe("DeadLettered");
        deadLetter.State["EventType"].ShouldBe((await SingleMessageAsync()).Type);
        deadLetter.State["LastError"].ToString().ShouldContain("selective failure");
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxLoggingTests : OutboxLoggingTestsBase
{
    public SqlServerOutboxLoggingTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxLoggingTests : OutboxLoggingTestsBase
{
    public PostgreSqlOutboxLoggingTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxBatchRunnerTests : OutboxBatchRunnerTestsBase
{
    public SqlServerOutboxBatchRunnerTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxBatchRunnerTests : OutboxBatchRunnerTestsBase
{
    public PostgreSqlOutboxBatchRunnerTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxIdempotencyDeliveryTests : OutboxIdempotencyDeliveryTestsBase
{
    public SqlServerOutboxIdempotencyDeliveryTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxIdempotencyDeliveryTests : OutboxIdempotencyDeliveryTestsBase
{
    public PostgreSqlOutboxIdempotencyDeliveryTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxAtomicHandlerTests : OutboxAtomicHandlerTestsBase
{
    public SqlServerOutboxAtomicHandlerTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxAtomicHandlerTests : OutboxAtomicHandlerTestsBase
{
    public PostgreSqlOutboxAtomicHandlerTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxAmbientContextTests : OutboxAmbientContextTestsBase
{
    public SqlServerOutboxAmbientContextTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxAmbientContextTests : OutboxAmbientContextTestsBase
{
    public PostgreSqlOutboxAmbientContextTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxFollowUpTests : OutboxFollowUpTestsBase
{
    public SqlServerOutboxFollowUpTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxFollowUpTests : OutboxFollowUpTestsBase
{
    public PostgreSqlOutboxFollowUpTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxShutdownTests : OutboxShutdownTestsBase
{
    public SqlServerOutboxShutdownTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxShutdownTests : OutboxShutdownTestsBase
{
    public PostgreSqlOutboxShutdownTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
