using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Infrastructure.Data;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data;

public class UnitOfWorkTests : InfrastructureTestBase
{
    [Fact]
    public async Task CommitAsync_ShouldPersistData()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Normal("Luffy", 150_000_000);

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            dbContext.Add(pirate);
            await unitOfWork.CommitAsync();

            var loaded = await dbContext
                .Set<Pirate>()
                .FirstOrDefaultAsync(p => p.Name == "Luffy");

            loaded.ShouldNotBeNull();
            loaded.Bounty.ShouldBe(new Bounty(150_000_000));
            pirate.ShouldBe(loaded);
        });
    }

    [Fact]
    public async Task CommitAsync_ShouldPersistData_UsingRepositories()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Normal("Luffy", 150_000_000);

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var loaded = await repository.GetByIdAsync(pirate.Id);

            loaded.ShouldNotBeNull();
            loaded.Bounty.ShouldBe(new Bounty(150_000_000));
            pirate.ShouldBe(loaded);
        });
    }
}