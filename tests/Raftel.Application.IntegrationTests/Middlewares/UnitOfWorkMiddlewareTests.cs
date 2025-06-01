using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Application.IntegrationTests.Middlewares;

public class UnitOfWorkMiddlewareTests : IntegrationTestBase
{
    [Fact]
    public async Task CommitAsync_ShouldPersistData_UsingMiddleware()
    {
        await ExecuteScopedAsync(async sp =>
        {
            CurrentUser.AddPermission(PiratesPermissions.Management);
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