using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Outbox;

/// <summary>Default options: the framework behaves exactly as it did before the outbox existed.</summary>
public abstract class OutboxDisabledTestsBase : InfrastructureTestBase
{
    private readonly HandlerLog _log = new();

    protected OutboxDisabledTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddRaftelApplication(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly));
        services.AddSingleton(_log);
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
    }

    private static Pirate CrownedPirate()
    {
        var pirate = Pirate.Normal("Luffy", 150_000_000);
        pirate.FoundOnePiece();
        return pirate;
    }

    [Fact]
    public async Task Commit_ShouldRunTheHandlerInProcess_AndStoreNoOutboxMessage()
    {
        await ExecuteScopedAsync(async sp =>
        {
            await sp.GetRequiredService<IPirateRepository>().AddAsync(CrownedPirate());
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();

            _log.Calls.ShouldHaveSingleItem();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            (await dbContext.OutboxMessage.IgnoreQueryFilters().CountAsync()).ShouldBe(0);
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxDisabledTests : OutboxDisabledTestsBase
{
    public SqlServerOutboxDisabledTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxDisabledTests : OutboxDisabledTestsBase
{
    public PostgreSqlOutboxDisabledTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
