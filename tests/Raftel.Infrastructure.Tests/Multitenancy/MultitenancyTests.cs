using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Multitenancy;

public class MultitenancyTests : InfrastructureTestBase
{
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
    public async Task ListAllAsync_ShouldReturnAllEntities_WhenNoTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

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

            var allPirates = await repository.ListAllAsync();
            allPirates.Count.ShouldBe(2);
            allPirates.ShouldContain(luffy);
            allPirates.ShouldContain(kaido);
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
    public async Task GetByIdAsync_ShouldReturnEntity_WhenNoTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenantId = Guid.NewGuid();

            var luffy = Pirate.Normal("Luffy", 500_000_000);

            using (currentTenant.Change(tenantId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            var pirateWithoutTenant = await repository.GetByIdAsync(luffy.Id);
            pirateWithoutTenant.ShouldNotBeNull();
            pirateWithoutTenant.ShouldBe(luffy);
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
}