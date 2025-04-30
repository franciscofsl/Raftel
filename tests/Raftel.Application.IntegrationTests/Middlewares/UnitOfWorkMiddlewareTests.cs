using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Tests.Common.Application.Pirates.CreatePirate;
using Raftel.Tests.Common.Domain;
using Shouldly;

namespace Raftel.Application.IntegrationTests.Middlewares;

public class UnitOfWorkMiddlewareTests : IntegrationTestBase
{
    [Fact]
    public async Task CommitAsync_ShouldPersistData_UsingMiddleware()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetService<ICommandDispatcher>();

            var command = new CreatePirateCommand("Ace", 9514361);
            var result = await commandDispatcher!.DispatchAsync(command);
            result.IsSuccess.ShouldBeTrue();

            var repository = sp.GetRequiredService<IPirateRepository>();

            var loaded = await repository.ListAllAsync();
            loaded.ShouldHaveSingleItem();
            loaded.ShouldContain(_ => _.Name == command.Name && _.Bounty == command.Bounty);
        });
    }
}