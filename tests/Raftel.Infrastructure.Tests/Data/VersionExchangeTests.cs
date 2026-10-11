using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Exceptions;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Infrastructure.Tests.Data;

public abstract class VersionExchangeTestsBase : InfrastructureTestBase
{
    protected VersionExchangeTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task VersionOf_ForTrackedVersionedEntity_IsNonEmpty_AndChangesAfterAnUpdate()
    {
        var tenantId = await SeedTenantAsync();

        var before = await ReadVersionAsync(tenantId);
        await UpdateDescriptionAsync(tenantId, "Pro");
        var after = await ReadVersionAsync(tenantId);

        before.ShouldNotBeNullOrWhiteSpace();
        after.ShouldNotBeNullOrWhiteSpace();
        after.ShouldNotBe(before);
    }

    [Fact]
    public async Task VersionOf_ForUnversionedOrUntrackedEntity_IsNull()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IEntityVersionReader>();
            var context = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Luffy", 100);
            context.Add(pirate);
            await context.SaveChangesAsync();

            reader.VersionOf(pirate).ShouldBeNull();
            reader.VersionOf(Tenant.Create("Untracked", "UNT", "x").Value).ShouldBeNull();
        });
    }

    [Fact]
    public async Task ExpectedVersion_MatchingCurrent_AllowsTheSave()
    {
        var tenantId = await SeedTenantAsync();
        var current = await ReadVersionAsync(tenantId);

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            sp.GetRequiredService<IExpectedVersion>().Expect(current);

            tenant.Description = "Pro";
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ExpectedVersion_Stale_RejectsTheSave_EvenForFreshlyLoadedEntity()
    {
        var tenantId = await SeedTenantAsync();
        var stale = await ReadVersionAsync(tenantId);
        await UpdateDescriptionAsync(tenantId, "Pro");

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            sp.GetRequiredService<IExpectedVersion>().Expect(stale);

            tenant.Name = "Acme Corp";

            await Should.ThrowAsync<ConcurrencyConflictException>(() => context.SaveChangesAsync());
        });

        await ExecuteScopedAsync(async sp =>
        {
            var tenant = await sp.GetRequiredService<TestingRaftelDbContext>().Set<Tenant>()
                .SingleAsync(t => t.Id == tenantId);

            tenant.Name.ShouldBe("Acme");
            tenant.Description.ShouldBe("Pro");
        });
    }

    [Theory]
    [InlineData("not-a-version")]
    [InlineData("AAAA")]
    [InlineData("-1")]
    public async Task ExpectedVersion_Undecodable_RejectsTheSave(string garbage)
    {
        var tenantId = await SeedTenantAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            sp.GetRequiredService<IExpectedVersion>().Expect(garbage);

            tenant.Name = "Acme Corp";

            await Should.ThrowAsync<ConcurrencyConflictException>(() => context.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task ExpectedVersion_IsEnforcedByExactlyOneSave()
    {
        var tenantId = await SeedTenantAsync();
        var current = await ReadVersionAsync(tenantId);

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var expected = sp.GetRequiredService<IExpectedVersion>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            expected.Expect(current);

            tenant.Description = "Pro";
            await context.SaveChangesAsync();
            expected.Value.ShouldBeNull();

            tenant.Description = "Enterprise";
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ExpectedVersion_IsKept_WhenTheSaveModifiesNoVersionedEntity()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var expected = sp.GetRequiredService<IExpectedVersion>();
            expected.Expect("some-version");

            context.Add(Pirate.Normal("Ace", 1));
            await context.SaveChangesAsync();

            expected.Value.ShouldBe("some-version");
        });
    }

    private async Task<TenantId> SeedTenantAsync()
    {
        TenantId tenantId = default!;

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = Tenant.Create("Acme", "ACME", "Free").Value;
            context.Add(tenant);
            await context.SaveChangesAsync();
            tenantId = tenant.Id;
        });

        return tenantId;
    }

    private async Task UpdateDescriptionAsync(TenantId tenantId, string description)
    {
        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            tenant.Description = description;
            await context.SaveChangesAsync();
        });
    }

    private async Task<string> ReadVersionAsync(TenantId tenantId)
    {
        string version = null;

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await context.Set<Tenant>().SingleAsync(t => t.Id == tenantId);
            version = sp.GetRequiredService<IEntityVersionReader>().VersionOf(tenant);
        });

        return version;
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerVersionExchangeTests : VersionExchangeTestsBase
{
    public SqlServerVersionExchangeTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlVersionExchangeTests : VersionExchangeTestsBase
{
    public PostgreSqlVersionExchangeTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
