using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Application.Commands;
using Raftel.Application.Tests.Common;
using Raftel.Application.Tests.Common.CreatePirate;
using Raftel.Domain.Tests.Common.Domain;
using Raftel.Domain.Tests.Common.Domain.ValueObjects;
using Raftel.Infrastructure.Tests.Data.Common;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data;

public class UnitOfWorkTests : DataTestBase
{
    [Fact]
    public async Task CommitAsync_ShouldPersistData()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Create("Luffy", 150_000_000);

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
            var pirate = Pirate.Create("Luffy", 150_000_000);

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

    [Fact]
    public async Task CommitAsync_ShouldPersistData_UsingMiddleware()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetService<ICommandDispatcher>();

            var command = new CreatePirateCommand("Ace", 9514361);
            var result = await commandDispatcher.DispatchAsync(command);
            result.IsSuccess.ShouldBeTrue();

            var repository = sp.GetRequiredService<IPirateRepository>();

            var loaded = await repository.ListAllAsync();
            loaded.ShouldHaveSingleItem();
            loaded.ShouldContain(_ => _.Name == command.Name && _.Bounty == command.Bounty);
        });
    }
}