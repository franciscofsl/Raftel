using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Tests.Common.Domain;
using Raftel.Tests.Common.Domain.ValueObjects;
using Raftel.Tests.Common.Infrastructure;
using Shouldly;

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
}