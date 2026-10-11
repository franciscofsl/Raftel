using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Auditing;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Auditing;
using Raftel.Domain.Features.Tenants;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Auditing;

public abstract class VersionAuditingTestsBase : InfrastructureTestBase
{
    protected VersionAuditingTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser());
        services.AddSingleton(new AuditOptions().Audit<Pirate>().Audit<Ship>().Audit<Tenant>());
    }

    [Fact]
    public async Task Create_OfVersionTrackedEntity_DoesNotAuditTheVersion()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            dbContext.Add(Tenant.Create("Acme", "ACME", "Free").Value);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();

            var entityChange = await LatestEntityChangeAsync(dbContext, AuditChangeType.Created);

            entityChange.AffectedProperties.Select(p => p.PropertyName).ShouldContain("Name");
            entityChange.AffectedProperties.ShouldNotContain(p => p.PropertyName == ShadowPropertyNames.Version);
        });
    }

    [Fact]
    public async Task Update_OfVersionTrackedEntity_AuditsOnlyBusinessProperties()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var tenant = Tenant.Create("Acme", "ACME", "Free").Value;
            dbContext.Add(tenant);
            await unitOfWork.CommitAsync();

            tenant.Description = "Pro";
            await unitOfWork.CommitAsync();

            var entityChange = await LatestEntityChangeAsync(dbContext, AuditChangeType.Updated);

            entityChange.AffectedProperties.Select(p => p.PropertyName).ShouldBe(["Description"]);
        });
    }

    private static async Task<EntityChange> LatestEntityChangeAsync(TestingRaftelDbContext dbContext, string changeType)
    {
        var auditLog = await dbContext.AuditLog
            .Include(a => a.EntityChanges)
            .ThenInclude(c => c.AffectedProperties)
            .Where(a => a.EntityChanges.Any(c => c.ChangeType == changeType))
            .OrderByDescending(a => a.Timestamp)
            .FirstAsync();

        return auditLog.EntityChanges.Single(c => c.ChangeType == changeType);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string UserName => "TestUser";
        public IEnumerable<string> Roles => new List<string>();

        public bool HasPermission(string permission) => true;
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerVersionAuditingTests : VersionAuditingTestsBase
{
    public SqlServerVersionAuditingTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlVersionAuditingTests : VersionAuditingTestsBase
{
    public PostgreSqlVersionAuditingTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
