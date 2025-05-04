using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Shouldly;
using Xunit.Sdk;

namespace Raftel.Infrastructure.Tests.Data;

public class EfRepositoryTests : InfrastructureTestBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldRetrieveExpectedData()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Create("Luffy", 150_000_000);

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var loaded = await repository.GetByIdAsync(pirate.Id);
            loaded.ShouldBe(pirate);
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldRetrieveExpectedData()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            await repository.AddAsync(Mugiwara.Brook);
            await repository.AddAsync(Mugiwara.Nami);
            await unitOfWork.CommitAsync();

            var pirates = await repository.ListAllAsync();
            pirates.Count.ShouldBe(2);
            pirates.ShouldContain(Mugiwara.Brook);
            pirates.ShouldContain(Mugiwara.Nami);
        });
    }

    [Fact]
    public async Task Update_ShouldModifyEntityCorrectly()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            var pirate = Pirate.Create("Sanji", 77_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            pirate.Bounty = 150_000_000;

            repository.Update(pirate);
            await unitOfWork.CommitAsync();

            var updated = await repository.GetByIdAsync(pirate.Id);
            updated!.Bounty.ShouldBe(new Bounty(150_000_000));
        });
    }

    [Fact]
    public async Task Remove_ShouldDeleteEntity()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            var pirate = Pirate.Create("Robin", 79_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            repository.Remove(pirate);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(pirate.Id);
            deleted.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Remove_ShouldDeleteEntity_WithSoftDelete()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IShipRepository>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var ship = Ship.Create("Thousand Sunny");
            await repository.AddAsync(ship);
            await unitOfWork.CommitAsync();

            repository.Remove(ship);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(ship.Id);
            deleted.ShouldBeNull();

            using (dataFilter.Disable<ISoftDeleteFilter>())
            {
                var deletedShip = await repository.GetByIdAsync(ship.Id);
                deletedShip.ShouldNotBeNull();
            }
        });
    }

    [Fact]
    public async Task Remove_ShouldSetIsDeletedTrue_WhenEntityIsSoftDeleted()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IShipRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var ship = Ship.Create("Going Merry");
            await repository.AddAsync(ship);
            await unitOfWork.CommitAsync();

            repository.Remove(ship);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(ship);
            entry.Property(ShadowPropertyNames.IsDeleted).CurrentValue.ShouldBe(true);
        });
    }
}