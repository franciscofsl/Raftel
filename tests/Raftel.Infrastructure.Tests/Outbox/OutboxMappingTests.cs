using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Outbox;
using Raftel.Infrastructure.Data.Auditing;

namespace Raftel.Infrastructure.Tests.Outbox;

public abstract class OutboxMappingTestsBase : InfrastructureTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    protected OutboxMappingTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton(new AuditOptions().Audit<Pirate>().Audit<OutboxMessage>());
    }

    [Fact]
    public async Task OutboxMessage_ShouldRoundTrip_ThroughTheContext()
    {
        var tenantId = Guid.NewGuid();
        var message = OutboxMessage.Create("Demo.Event, Demo", "{\"a\":1}", Now, tenantId, "corr-1");
        message.Claim(Now.AddMinutes(1));
        message.MarkFailed(Now, "boom", Now.AddSeconds(10));

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.OutboxMessage.Add(message);
            await dbContext.SaveChangesAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.OutboxMessage.IgnoreQueryFilters().SingleAsync(m => m.Id == message.Id);

            loaded.Type.ShouldBe("Demo.Event, Demo");
            loaded.Content.ShouldBe("{\"a\":1}");
            loaded.OccurredOn.ShouldBe(Now);
            loaded.TenantId.ShouldBe(tenantId);
            loaded.CorrelationId.ShouldBe("corr-1");
            loaded.AttemptCount.ShouldBe(1);
            loaded.Error.ShouldBe("boom");
            loaded.NextAttemptOn.ShouldBe(Now.AddSeconds(10));
            loaded.LockedUntil.ShouldBeNull();
            loaded.ProcessedOn.ShouldBeNull();
        });
    }

    [Fact]
    public async Task SavingOutboxMessage_ShouldNotProduceAnAuditEntry_EvenWhenConfiguredAsAudited()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.Add(Pirate.Normal("Usopp", 500_000));
            dbContext.OutboxMessage.Add(OutboxMessage.Create("Demo.Event, Demo", "{}", Now, null, null));
            await dbContext.SaveChangesAsync();

            var auditLog = await dbContext.AuditLog.Include(a => a.EntityChanges).SingleAsync();

            auditLog.EntityChanges.ShouldNotBeEmpty();
            auditLog.EntityChanges.ShouldAllBe(c => c.EntityFullName == typeof(Pirate).FullName);
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerOutboxMappingTests : OutboxMappingTestsBase
{
    public SqlServerOutboxMappingTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlOutboxMappingTests : OutboxMappingTestsBase
{
    public PostgreSqlOutboxMappingTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
