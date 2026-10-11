using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Application.IntegrationTests;

[Collection(IntegrationSqlServerTestCollection.Name)]
public sealed class LostUpdateTests : IntegrationTestBase
{
    private readonly SqlServerTestContainerFixture _fixture;

    public LostUpdateTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(LostUpdateTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(ConcurrencyMiddleware<>));
            cfg.AddCommandMiddleware(typeof(ConcurrencyMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString);
        services.AddSingleton<ICurrentUser>(CurrentUser);
    }

    [Fact]
    public async Task Command_WhenAnotherWriterChangedTheAggregateMeanwhile_ReturnsConflictFailure()
    {
        var tenantId = await SeedTenantAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new EditTenantRacingCommand(tenantId, WritePirate: null));

            result.IsFailure.ShouldBeTrue();
            result.Error.Type.ShouldBe(ErrorType.Conflict);
            result.Error.Code.ShouldBe(ConcurrencyErrors.ConflictCode);
        });

        await ExecuteScopedAsync(async sp =>
        {
            var tenant = await sp.GetRequiredService<TestingRaftelDbContext>().Set<Tenant>()
                .SingleAsync(t => t.Id == tenantId);

            tenant.Description.ShouldBe("Changed by the other writer");
            tenant.Name.ShouldBe("Acme");
        });
    }

    [Fact]
    public async Task Command_ThatPersistedAnAggregate_ThenConflicts_PersistsNothing()
    {
        var tenantId = await SeedTenantAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new EditTenantRacingCommand(tenantId, WritePirate: "Ace"));

            result.Error.Code.ShouldBe(ConcurrencyErrors.ConflictCode);
        });

        await ExecuteScopedAsync(async sp =>
        {
            var pirate = await sp.GetRequiredService<TestingRaftelDbContext>().Set<Pirate>()
                .FirstOrDefaultAsync(p => p.Name == "Ace");

            pirate.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Command_WithoutAConcurrentWriter_Succeeds()
    {
        var tenantId = await SeedTenantAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new EditTenantRacingCommand(tenantId, WritePirate: null, Race: false));

            result.IsSuccess.ShouldBeTrue();
        });
    }

    private async Task<TenantId> SeedTenantAsync()
    {
        TenantId tenantId = default!;

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = Tenant.Create("Acme", "ACME", "Free").Value;
            context.Add(tenant);
            await context.SaveChangesAsync();
            tenantId = tenant.Id;
        });

        return tenantId;
    }
}

internal sealed record EditTenantRacingCommand(TenantId TenantId, string? WritePirate, bool Race = true) : ICommand;

internal sealed class EditTenantRacingCommandHandler(
    TestingRaftelDbContext dbContext,
    IPirateRepository pirateRepository,
    IUnitOfWork unitOfWork,
    IServiceScopeFactory scopeFactory) : ICommandHandler<EditTenantRacingCommand>
{
    public async Task<Result> HandleAsync(EditTenantRacingCommand request, CancellationToken token = default)
    {
        if (request.WritePirate is not null)
        {
            await pirateRepository.AddAsync(Pirate.Normal(request.WritePirate, 1), token);
            await unitOfWork.CommitAsync(token);
        }

        var tenant = await dbContext.Set<Tenant>().SingleAsync(t => t.Id == request.TenantId, token);

        if (request.Race)
        {
            using var otherScope = scopeFactory.CreateScope();
            var otherContext = otherScope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
            var sameTenant = await otherContext.Set<Tenant>().SingleAsync(t => t.Id == request.TenantId, token);
            sameTenant.Description = "Changed by the other writer";
            await otherContext.SaveChangesAsync(token);
        }

        tenant.Name = "Acme Corp";
        return Result.Success();
    }
}
