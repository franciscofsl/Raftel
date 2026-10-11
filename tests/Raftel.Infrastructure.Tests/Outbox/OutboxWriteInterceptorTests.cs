using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public abstract class OutboxWriteInterceptorTestsBase : OutboxTestBase
{
    protected OutboxWriteInterceptorTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
    }

    private static Pirate CrownedPirate(string name)
    {
        var pirate = Pirate.Normal(name, 100_000_000);
        pirate.FoundOnePiece();
        return pirate;
    }

    private async Task<List<OutboxMessage>> StoredMessagesAsync()
    {
        List<OutboxMessage> messages = null;
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            messages = await dbContext.OutboxMessage.IgnoreQueryFilters().ToListAsync();
        });
        return messages;
    }

    [Fact]
    public async Task Commit_ShouldStoreOnePendingMessage_WithTheEventTenantAndCorrelation_AndClearTheEvents()
    {
        var tenantId = Guid.NewGuid();
        Pirate pirate = null;
        PirateCrownedKing raised = null;

        await ExecuteScopedAsync(async sp =>
        {
            using var tenant = sp.GetRequiredService<ICurrentTenant>().Change(tenantId);
            using var correlation = sp.GetRequiredService<ICorrelationScope>().Begin("corr-42");

            pirate = CrownedPirate("Luffy");
            raised = pirate.DomainEvents.OfType<PirateCrownedKing>().Single();
            await sp.GetRequiredService<IPirateRepository>().AddAsync(pirate);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        var message = (await StoredMessagesAsync()).ShouldHaveSingleItem();
        message.ProcessedOn.ShouldBeNull();
        message.AttemptCount.ShouldBe(0);
        message.OccurredOn.ShouldBe(raised.OccurredOn);
        message.TenantId.ShouldBe(tenantId);
        message.CorrelationId.ShouldBe("corr-42");
        message.Type.ShouldBe("Raftel.Demo.Domain.Pirates.Events.PirateCrownedKing, Raftel.Demo.Domain");
        message.Content.ShouldContain(((Guid)pirate.Id).ToString());
        pirate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Commit_ShouldNotInvokeTheHandlers()
    {
        await ExecuteScopedAsync(async sp =>
        {
            await sp.GetRequiredService<IPirateRepository>().AddAsync(CrownedPirate("Zoro"));
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        Log.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Commit_ShouldStoreOneMessagePerEvent()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            await repository.AddAsync(CrownedPirate("Luffy"));
            await repository.AddAsync(CrownedPirate("Zoro"));
            await repository.AddAsync(CrownedPirate("Sanji"));
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        (await StoredMessagesAsync()).Count.ShouldBe(3);
    }

    [Fact]
    public async Task Commit_ShouldStoreNoTenant_WhenNoneIsCurrent()
    {
        await ExecuteScopedAsync(async sp =>
        {
            await sp.GetRequiredService<IPirateRepository>().AddAsync(CrownedPirate("Nami"));
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        (await StoredMessagesAsync()).ShouldHaveSingleItem().TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task RolledBackTransaction_ShouldStoreNoMessage()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var transaction = await unitOfWork.BeginTransactionAsync();

            await sp.GetRequiredService<IPirateRepository>().AddAsync(CrownedPirate("Robin"));
            await unitOfWork.CommitAsync();
            await transaction.RollbackAsync();
        });

        (await StoredMessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task SynchronousSaveChanges_ShouldStoreTheMessageToo()
    {
        await ExecuteScopedAsync(sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.Add(CrownedPirate("Chopper"));
            dbContext.SaveChanges();
            return Task.CompletedTask;
        });

        (await StoredMessagesAsync()).ShouldHaveSingleItem();
    }
}

public abstract class OutboxWriteInterceptorFailureTestsBase : OutboxTestBase
{
    protected OutboxWriteInterceptorFailureTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<IOutboxSerializer, UnserializableEvents>();
    }

    [Fact]
    public async Task Commit_ShouldFailAndPersistNothing_WhenAnEventCannotBeSerialized()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Normal("Brook", 83_000_000);
            pirate.FoundOnePiece();
            await sp.GetRequiredService<IPirateRepository>().AddAsync(pirate);

            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => sp.GetRequiredService<IUnitOfWork>().CommitAsync());

            exception.Message.ShouldContain(nameof(PirateCrownedKing));
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            (await dbContext.Set<Pirate>().CountAsync()).ShouldBe(0);
            (await dbContext.OutboxMessage.IgnoreQueryFilters().CountAsync()).ShouldBe(0);
        });
    }

    private sealed class UnserializableEvents : IOutboxSerializer
    {
        public SerializedDomainEvent Serialize(IDomainEvent domainEvent) =>
            throw new InvalidOperationException($"Domain event '{domainEvent.GetType().Name}' cannot be serialized.");

        public Result<IDomainEvent> Deserialize(string type, string content) =>
            throw new NotSupportedException();
    }
}

public abstract class OutboxWriteInterceptorNoHandlerTestsBase : OutboxTestBase
{
    protected OutboxWriteInterceptorNoHandlerTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override bool ScanDemoHandlers => false;

    [Fact]
    public async Task Commit_ShouldStoreNothing_ButStillClearTheEvents_WhenTheEventHasNoHandler()
    {
        var pirate = Pirate.Normal("Franky", 94_000_000);
        pirate.FoundOnePiece();

        await ExecuteScopedAsync(async sp =>
        {
            await sp.GetRequiredService<IPirateRepository>().AddAsync(pirate);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            (await dbContext.OutboxMessage.IgnoreQueryFilters().CountAsync()).ShouldBe(0);
        });
        pirate.DomainEvents.ShouldBeEmpty();
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxWriteInterceptorTests : OutboxWriteInterceptorTestsBase
{
    public SqlServerOutboxWriteInterceptorTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxWriteInterceptorTests : OutboxWriteInterceptorTestsBase
{
    public PostgreSqlOutboxWriteInterceptorTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxWriteInterceptorFailureTests : OutboxWriteInterceptorFailureTestsBase
{
    public SqlServerOutboxWriteInterceptorFailureTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxWriteInterceptorFailureTests : OutboxWriteInterceptorFailureTestsBase
{
    public PostgreSqlOutboxWriteInterceptorFailureTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxWriteInterceptorNoHandlerTests : OutboxWriteInterceptorNoHandlerTestsBase
{
    public SqlServerOutboxWriteInterceptorNoHandlerTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxWriteInterceptorNoHandlerTests : OutboxWriteInterceptorNoHandlerTestsBase
{
    public PostgreSqlOutboxWriteInterceptorNoHandlerTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
