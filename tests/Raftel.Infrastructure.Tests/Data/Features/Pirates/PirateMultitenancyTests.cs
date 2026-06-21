using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Data.Features.Pirates;

public abstract class PirateMultitenancyTestsBase : InfrastructureTestBase
{
    protected PirateMultitenancyTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPirateIsFromDifferentTenant()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenantAId = Guid.NewGuid();
            var tenantBId = Guid.NewGuid();

            currentTenant.Change(tenantAId);

            var pirate = Pirate.Normal("Luffy", 150_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            currentTenant.Change(tenantBId);

            var loaded = await repository.GetByIdAsync(pirate.Id);
            loaded.ShouldBeNull();

            using (dataFilter.Disable<ITenantFilter>())
            {
                var loadedWithoutFilter = await repository.GetByIdAsync(pirate.Id);
                loadedWithoutFilter.ShouldNotBeNull();
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnOnlyCurrentTenantPirates()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenantAId = Guid.NewGuid();
            var tenantBId = Guid.NewGuid();

            currentTenant.Change(tenantAId);
            var luffy = Pirate.Normal("Luffy", 150_000_000);
            await repository.AddAsync(luffy);
            await unitOfWork.CommitAsync();

            currentTenant.Change(tenantBId);
            var zoro = Pirate.Normal("Zoro", 120_000_000);
            await repository.AddAsync(zoro);
            await unitOfWork.CommitAsync();

            var piratesOfTenantB = await repository.ListAllAsync();
            piratesOfTenantB.Count.ShouldBe(1);
            piratesOfTenantB.ShouldContain(zoro);
            piratesOfTenantB.ShouldNotContain(luffy);
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerPirateMultitenancyTests : PirateMultitenancyTestsBase
{
    public SqlServerPirateMultitenancyTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlPirateMultitenancyTests : PirateMultitenancyTestsBase
{
    public PostgreSqlPirateMultitenancyTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
