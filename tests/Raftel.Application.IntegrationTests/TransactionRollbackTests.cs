using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Application.Queries;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Auditing;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Application.IntegrationTests;

[Collection(IntegrationSqlServerTestCollection.Name)]
public sealed class TransactionRollbackTests : IntegrationTestBase
{
    private readonly SqlServerTestContainerFixture _fixture;

    public TransactionRollbackTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(TransactionRollbackTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(PermissionAuthorizationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(AuditLogMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString);
        services.AddSingleton<ICurrentUser>(CurrentUser);
    }

    [Fact]
    public async Task Command_ThatCommitsFirstAggregate_ThenFailsOnSecond_ShouldPersistNoRows()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();

            var result = await commandDispatcher.DispatchAsync(
                new CreateTwoAggregatesAndFailCommand("Ace", "Moby Dick"));

            result.IsFailure.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Ace");
            var ship = await dbContext.Set<Ship>().FirstOrDefaultAsync(s => s.Name == "Moby Dick");

            pirate.ShouldBeNull();
            ship.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Command_ThatCommitsFirstAggregate_ThenFailsOnSecond_ShouldPersistNoAuditLog()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();

            await commandDispatcher.DispatchAsync(new CreateTwoAggregatesAndFailCommand("Marco", "Moby Dick II"));
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var auditLogged = await dbContext.Set<AuditLog>()
                .AnyAsync(log => log.Command == typeof(CreateTwoAggregatesAndFailCommand).FullName);

            auditLogged.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Command_ThatSucceeds_ShouldPersistDomainEventHandlerWrite()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();

            var result = await commandDispatcher.DispatchAsync(new CrownPirateKingCommand("Roger", ShouldFail: false));

            result.IsSuccess.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Roger");
            var shipCount = await dbContext.Set<Ship>().CountAsync();

            pirate.ShouldNotBeNull();
            pirate.IsKing.ShouldBeTrue();
            shipCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Command_ThatFailsAfterDomainEventDispatch_ShouldRevertDomainEventHandlerWrite()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();

            var result = await commandDispatcher.DispatchAsync(new CrownPirateKingCommand("Whitebeard", ShouldFail: true));

            result.IsFailure.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Whitebeard");
            var shipCount = await dbContext.Set<Ship>().CountAsync();

            pirate.ShouldBeNull();
            shipCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Query_ShouldNotOpenTransaction()
    {
        CurrentUser.AddPermission(PiratesPermissions.View);

        await ExecuteScopedAsync(async sp =>
        {
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            unitOfWork.HasActiveTransaction.ShouldBeFalse();

            var result = await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(
                new GetPirateByFilterQuery(string.Empty, null));

            result.IsSuccess.ShouldBeTrue();
            unitOfWork.HasActiveTransaction.ShouldBeFalse();
        });
    }
}

internal sealed record CreateTwoAggregatesAndFailCommand(string PirateName, string ShipName) : ICommand;

internal sealed class CreateTwoAggregatesAndFailCommandHandler(
    IPirateRepository pirateRepository,
    IShipRepository shipRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTwoAggregatesAndFailCommand>
{
    public async Task<Result> HandleAsync(CreateTwoAggregatesAndFailCommand request, CancellationToken token = default)
    {
        var pirate = Pirate.Normal(request.PirateName, 1);
        await pirateRepository.AddAsync(pirate, token);
        await unitOfWork.CommitAsync(token);

        var ship = Ship.Create(request.ShipName);
        await shipRepository.AddAsync(ship, token);

        return Result.Failure(Error.Failure("Test.SimulatedFailure", "Simulated failure after the first commit."));
    }
}

internal sealed record CrownPirateKingCommand(string PirateName, bool ShouldFail) : ICommand;

internal sealed class CrownPirateKingCommandHandler(
    IPirateRepository pirateRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CrownPirateKingCommand>
{
    public async Task<Result> HandleAsync(CrownPirateKingCommand request, CancellationToken token = default)
    {
        var pirate = Pirate.Normal(request.PirateName, 1);
        await pirateRepository.AddAsync(pirate, token);
        pirate.FoundOnePiece();
        await unitOfWork.CommitAsync(token);

        return request.ShouldFail
            ? Result.Failure(Error.Failure("Test.SimulatedFailure", "Simulated failure after coronation."))
            : Result.Success();
    }
}

internal sealed class ShipBuildingOnCoronationHandler(IShipRepository shipRepository, IUnitOfWork unitOfWork)
    : IDomainEventHandler<PirateCrownedKing>
{
    public async Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        await shipRepository.AddAsync(Ship.Create($"Flagship-{domainEvent.PirateId}"), cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
