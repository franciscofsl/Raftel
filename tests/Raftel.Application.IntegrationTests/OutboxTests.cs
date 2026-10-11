using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Outbox;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Tests;
using Raftel.Infrastructure.Tests.Outbox;
using Shouldly;

namespace Raftel.Application.IntegrationTests;

/// <summary>
/// Exercises the outbox through the real command pipeline (transaction + unit-of-work middleware) and the
/// real background processor, starting it the way a host would.
/// </summary>
[Collection(IntegrationSqlServerTestCollection.Name)]
public sealed class OutboxEnabledPipelineTests : IntegrationTestBase
{
    private readonly SqlServerTestContainerFixture _fixture;
    private readonly HandlerLog _log = new();

    public OutboxEnabledPipelineTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(OutboxEnabledPipelineTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(PermissionAuthorizationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString, DatabaseProvider.SqlServer, options =>
        {
            options.Enabled = true;
            options.PollingInterval = TimeSpan.FromMilliseconds(50);
            options.BaseRetryDelay = TimeSpan.FromMilliseconds(50);
            options.MaxRetryDelay = TimeSpan.FromMilliseconds(100);
        });
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton(_log);
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, RecordingHandler>();
    }

    private async Task<IHostedService> StartProcessorAsync()
    {
        var processor = ServiceProvider.GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "OutboxProcessor");
        await processor.StartAsync(CancellationToken.None);
        return processor;
    }

    private async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The condition was not met in time.");
    }

    private async Task<List<OutboxMessage>> MessagesAsync()
    {
        var messages = new List<OutboxMessage>();
        await ExecuteScopedAsync(async sp =>
        {
            messages = await sp.GetRequiredService<TestingRaftelDbContext>().OutboxMessage
                .IgnoreQueryFilters().AsNoTracking().ToListAsync();
        });
        return messages;
    }

    [Fact]
    public async Task Command_ShouldSucceedAndPersist_EvenWhenAnEventHandlerAlwaysFails()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CrownPirateKingCommand("Roger", ShouldFail: false));

            result.IsSuccess.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            (await dbContext.Set<Pirate>().AnyAsync(p => p.Name == "Roger")).ShouldBeTrue();
            (await dbContext.Set<Ship>().CountAsync()).ShouldBe(0);
        });
        (await MessagesAsync()).ShouldHaveSingleItem().ProcessedOn.ShouldBeNull();
        _log.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailedCommand_ShouldLeaveNoOutboxMessage()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CrownPirateKingCommand("Whitebeard", ShouldFail: true));

            result.IsFailure.ShouldBeTrue();
        });

        (await MessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Processor_ShouldDeliverTheEventAfterTheCommand_InTheCommandsTenant()
    {
        var tenantId = Guid.NewGuid();

        await ExecuteScopedAsync(async sp =>
        {
            using var tenant = sp.GetRequiredService<ICurrentTenant>().Change(tenantId);

            await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CrownPirateKingCommand("Roger", ShouldFail: false));
        });

        var processor = await StartProcessorAsync();
        try
        {
            await WaitUntilAsync(async () => (await MessagesAsync()).All(m => m.ProcessedOn is not null));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        var call = _log.Calls.ShouldHaveSingleItem();
        call.Tenant.ShouldBe(tenantId);
        await ExecuteScopedAsync(async sp =>
        {
            (await sp.GetRequiredService<TestingRaftelDbContext>().Set<Ship>().CountAsync()).ShouldBe(1);
        });
    }
}

/// <summary>With the default options the pipeline keeps dispatching in-process, exactly as before.</summary>
[Collection(IntegrationSqlServerTestCollection.Name)]
public sealed class OutboxDisabledPipelineTests : IntegrationTestBase
{
    private readonly SqlServerTestContainerFixture _fixture;

    public OutboxDisabledPipelineTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(OutboxDisabledPipelineTests).Assembly);
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton(new HandlerLog());
        services.AddTransient<IDomainEventHandler<PirateCrownedKing>, ThrowingHandler>();
    }

    [Fact]
    public async Task HandlerFailure_ShouldStillPropagateToTheCaller_AndStoreNoOutboxMessage()
    {
        await ExecuteScopedAsync(async sp =>
        {
            // In-process dispatch surfaces the handler's exception (wrapped by reflection), as it always did.
            var exception = await Should.ThrowAsync<Exception>(() => sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CrownPirateKingCommand("Roger", ShouldFail: false)));

            exception.ToString().ShouldContain("handler exploded");
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            (await dbContext.OutboxMessage.IgnoreQueryFilters().CountAsync()).ShouldBe(0);
            (await dbContext.Set<Pirate>().AnyAsync(p => p.Name == "Roger")).ShouldBeFalse();
        });
    }
}
