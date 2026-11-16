using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Multitenancy;

public abstract class MultitenancyTestsBase : InfrastructureTestBase
{
    protected MultitenancyTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnOnlyCurrentTenantEntities_WhenTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();

            var luffy = Pirate.Normal("Luffy", 500_000_000);
            var zoro = Pirate.Normal("Zoro", 320_000_000);

            using (currentTenant.Change(tenant1Id))
            {
                await repository.AddAsync(luffy);
                await repository.AddAsync(zoro);
                await unitOfWork.CommitAsync();
            }

            var kaido = Pirate.Normal("Kaido", 800_000_000);
            var bigMom = Pirate.Normal("Big Mom", 900_000_000);

            using (currentTenant.Change(tenant2Id))
            {
                await repository.AddAsync(kaido);
                await repository.AddAsync(bigMom);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(tenant1Id))
            {
                var tenant1Pirates = await repository.ListAllAsync();
                tenant1Pirates.Count.ShouldBe(2);
                tenant1Pirates.ShouldContain(luffy);
                tenant1Pirates.ShouldContain(zoro);
                tenant1Pirates.ShouldNotContain(kaido);
                tenant1Pirates.ShouldNotContain(bigMom);
            }

            using (currentTenant.Change(tenant2Id))
            {
                var tenant2Pirates = await repository.ListAllAsync();
                tenant2Pirates.Count.ShouldBe(2);
                tenant2Pirates.ShouldContain(kaido);
                tenant2Pirates.ShouldContain(bigMom);
                tenant2Pirates.ShouldNotContain(luffy);
                tenant2Pirates.ShouldNotContain(zoro);
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_WhenNoTenantIsSet_ShouldReturnOnlyHostEntities()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, tenant1Pirate, tenant2Pirate) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();

            var pirates = await repository.ListAllAsync();

            pirates.Count.ShouldBe(1);
            pirates.ShouldContain(host);
            pirates.ShouldNotContain(tenant1Pirate);
            pirates.ShouldNotContain(tenant2Pirate);
        });
    }

    [Fact]
    public async Task ListAllAsync_WhenTenantIsSet_ShouldNotReturnHostEntities()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, tenant1Pirate, _) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            using (currentTenant.Change(TenantOf(sp, tenant1Pirate)))
            {
                var pirates = await repository.ListAllAsync();

                pirates.ShouldBe([tenant1Pirate]);
                pirates.ShouldNotContain(host);
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_WithAPredicateAndNoTenantSet_ShouldStillExcludeOtherTenantsEntities()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (_, tenant1Pirate, _) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();

            var pirates = await repository.ListAllAsync(p => p.Bounty > 0);

            pirates.ShouldNotContain(tenant1Pirate);
            pirates.Count.ShouldBe(1);
        });
    }

    [Fact]
    public async Task ListPagedAsync_WhenNoTenantIsSet_ShouldPageAndCountOnlyHostEntities()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, _, _) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();

            var page = await repository.ListPagedAsync(PageRequest.Create(1, 50).Value);

            page.TotalCount.ShouldBe(1);
            page.Items.ShouldBe([host]);
        });
    }

    [Fact]
    public async Task ListAllAsync_WhenNoTenantIsSetAndTenantFilterIsDisabled_ShouldReturnEveryTenantsEntities()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, tenant1Pirate, tenant2Pirate) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            using (dataFilter.Disable<ITenantFilter>())
            {
                var pirates = await repository.ListAllAsync();

                pirates.Count.ShouldBe(3);
                pirates.ShouldContain(host);
                pirates.ShouldContain(tenant1Pirate);
                pirates.ShouldContain(tenant2Pirate);
            }

            (await repository.ListAllAsync()).ShouldBe([host]);
        });
    }

    [Fact]
    public async Task UsersRepository_WhenNoTenantIsSet_ShouldNotExposeUsersOfATenant()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var users = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var tenantUser = User.Create("tenant.user@test.com", "Tenant", "User");
            tenantUser.BindTo(Guid.NewGuid().ToString());

            using (currentTenant.Change(Guid.NewGuid()))
            {
                await users.AddAsync(tenantUser);
                await unitOfWork.CommitAsync();
            }

            (await users.ListAllAsync()).ShouldNotContain(tenantUser);
            (await users.GetByIdAsync(tenantUser.Id)).ShouldBeNull();
            (await users.EmailIsUniqueAsync("tenant.user@test.com", CancellationToken.None)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityBelongsToAnotherTenant()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();

            var luffy = Pirate.Normal("Luffy", 500_000_000);

            using (currentTenant.Change(tenant1Id))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(tenant2Id))
            {
                var pirateFromAnotherTenant = await repository.GetByIdAsync(luffy.Id);
                pirateFromAnotherTenant.ShouldBeNull();
            }

            using (currentTenant.Change(tenant1Id))
            {
                var pirateFromSameTenant = await repository.GetByIdAsync(luffy.Id);
                pirateFromSameTenant.ShouldNotBeNull();
                pirateFromSameTenant.ShouldBe(luffy);
            }
        });
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoTenantIsSet_ShouldNotReturnAnotherTenantsEntity()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, tenant1Pirate, _) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();

            (await repository.GetByIdAsync(tenant1Pirate.Id)).ShouldBeNull();
            (await repository.GetByIdAsync(host.Id)).ShouldBe(host);
        });
    }

    [Fact]
    public async Task GetByIdAsync_WhenTenantIsSet_ShouldNotReturnAHostEntity()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var (host, tenant1Pirate, _) = await SeedHostAndTwoTenantsAsync(sp);
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            using (currentTenant.Change(TenantOf(sp, tenant1Pirate)))
            {
                (await repository.GetByIdAsync(host.Id)).ShouldBeNull();
            }
        });
    }

    [Fact]
    public async Task AddAsync_ShouldSetTenantId_WhenTenantIsActive()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var tenantId = Guid.NewGuid();
            var pirate = Pirate.Normal("Luffy", 500_000_000);

            using (currentTenant.Change(tenantId))
            {
                await repository.AddAsync(pirate);
                await unitOfWork.CommitAsync();

                var entry = dbContext.Entry(pirate);
                entry.Property(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe(tenantId);
            }
        });
    }

    [Fact]
    public async Task AddAsync_ShouldNotSetTenantId_WhenNoTenantIsActive()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Luffy", 500_000_000);

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(pirate);
            entry.Property(ShadowPropertyNames.TenantId).CurrentValue.ShouldBeNull();
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnAllEntities_WhenTenantFilterIsDisabled()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();

            var luffy = Pirate.Normal("Luffy", 500_000_000);
            var kaido = Pirate.Normal("Kaido", 800_000_000);

            using (currentTenant.Change(tenant1Id))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(tenant2Id))
            {
                await repository.AddAsync(kaido);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(tenant1Id))
            {
                using (dataFilter.Disable<ITenantFilter>())
                {
                    var allPirates = await repository.ListAllAsync();
                    allPirates.Count.ShouldBe(2);
                    allPirates.ShouldContain(luffy);
                    allPirates.ShouldContain(kaido);
                }
            }
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotChangeTenantId_WhenTenantIdIsModified()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();

            var pirate = Pirate.Normal("Luffy", 500_000_000);

            using (currentTenant.Change(tenant1Id))
            {
                await repository.AddAsync(pirate);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(tenant2Id))
            {
                var entry = dbContext.Entry(pirate);
                entry.Property(ShadowPropertyNames.TenantId).CurrentValue = tenant2Id;

                await unitOfWork.CommitAsync();

                entry.Property(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe(tenant1Id);
            }
        });
    }

    private static async Task<(Pirate Host, Pirate Tenant1, Pirate Tenant2)> SeedHostAndTwoTenantsAsync(IServiceProvider sp)
    {
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var repository = sp.GetRequiredService<IPirateRepository>();
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();

        var host = Pirate.Normal("Shanks", 4_048_900_000);
        await repository.AddAsync(host);
        await unitOfWork.CommitAsync();

        var tenant1Pirate = Pirate.Normal("Luffy", 500_000_000);
        using (currentTenant.Change(Guid.NewGuid()))
        {
            await repository.AddAsync(tenant1Pirate);
            await unitOfWork.CommitAsync();
        }

        var tenant2Pirate = Pirate.Normal("Kaido", 800_000_000);
        using (currentTenant.Change(Guid.NewGuid()))
        {
            await repository.AddAsync(tenant2Pirate);
            await unitOfWork.CommitAsync();
        }

        return (host, tenant1Pirate, tenant2Pirate);
    }

    private static Guid TenantOf(IServiceProvider sp, Pirate pirate) =>
        (Guid)sp.GetRequiredService<TestingRaftelDbContext>().Entry(pirate)
            .Property(ShadowPropertyNames.TenantId).CurrentValue!;
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerMultitenancyTests : MultitenancyTestsBase
{
    public SqlServerMultitenancyTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlMultitenancyTests : MultitenancyTestsBase
{
    public PostgreSqlMultitenancyTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}