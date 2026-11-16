using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Commands;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Tenants.CreateTenant;
using Raftel.Application.Middlewares;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Application.IntegrationTests;

[Collection(IntegrationSqlServerTestCollection.Name)]
public sealed class TenantSeedingTests : IntegrationTestBase
{
    private readonly SqlServerTestContainerFixture _fixture;
    private readonly TenantSeederSwitch _switch = new();

    public TenantSeedingTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateTenantCommand).Assembly);
            cfg.AddGlobalMiddleware(typeof(PermissionAuthorizationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
            cfg.AddCommandMiddleware(typeof(TransactionMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString);
        services.AddSingleton<ICurrentUser>(CurrentUser);

        services.RemoveAll<IDataSeeder>();
        services.Configure<SeedOptions>(o => o.FailFastOnError = false);
        services.AddSingleton(_switch);
        services.AddScoped<IDataSeeder, RolePerTenantSeeder>();
    }

    [Fact]
    public async Task CreateTenant_ShouldRunTenantSeedersInTheNewTenantsContext()
    {
        CurrentUser.AddPermission(TenantsPermissions.Management);

        await ExecuteScopedAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CreateTenantCommand("Alpha", "ALPHA", "Alpha tenant"));
            result.IsSuccess.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            var tenant = await db.Set<Tenant>().SingleAsync();
            var roles = await db.Set<Role>().IgnoreQueryFilters().ToListAsync();

            roles.Count.ShouldBe(1);
            roles[0].Name.ShouldBe($"role-{(Guid)tenant.Id}");
            db.Entry(roles[0]).Property<Guid?>(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe((Guid)tenant.Id);
        });
    }

    [Fact]
    public async Task CreateTenant_WhenATenantSeederFails_ShouldPersistNeitherTenantNorSeededData()
    {
        CurrentUser.AddPermission(TenantsPermissions.Management);
        _switch.Fail = true;

        await ExecuteScopedAsync(async sp =>
        {
            await Should.ThrowAsync<SeedingFailedException>(() => sp.GetRequiredService<ICommandDispatcher>()
                .DispatchAsync(new CreateTenantCommand("Beta", "BETA", "Beta tenant")));
        });

        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            (await db.Set<Tenant>().CountAsync()).ShouldBe(0);
            (await db.Set<Role>().IgnoreQueryFilters().CountAsync()).ShouldBe(0);
        });
    }

    [Fact]
    public async Task StartupSeeding_ShouldSeedEveryExistingTenantInItsOwnContext_AndKeepTheFirstWhenTheSecondFails()
    {
        Guid first = default, second = default;
        await ExecuteScopedAsync(async sp =>
        {
            var tenants = sp.GetRequiredService<ITenantsRepository>();
            var alpha = Tenant.Create("Alpha", "ALPHA", "a").Value;
            var beta = Tenant.Create("Beta", "BETA", "b").Value;
            await tenants.AddAsync(alpha);
            await tenants.AddAsync(beta);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
            first = (Guid)alpha.Id;
            second = (Guid)beta.Id;
        });

        await ExecuteScopedAsync(async db_sp => await db_sp.GetRequiredService<TestingRaftelDbContext>()
            .Set<Role>().IgnoreQueryFilters().ExecuteDeleteAsync());
        _switch.FailForTenant = second;

        Result result = Result.Success();
        await ExecuteScopedAsync(async sp => result = await sp.GetRequiredService<ISeedRunner>().RunAsync());

        result.IsFailure.ShouldBeTrue();
        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            var roles = await db.Set<Role>().IgnoreQueryFilters().ToListAsync();
            roles.Count.ShouldBe(1);
            db.Entry(roles[0]).Property<Guid?>(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe(first);
        });
    }

    private sealed class TenantSeederSwitch
    {
        public bool Fail { get; set; }
        public Guid? FailForTenant { get; set; }
    }

    private sealed class RolePerTenantSeeder(IRolesRepository roles, TenantSeederSwitch seederSwitch) : ITenantDataSeeder
    {
        public int Order => 10;
        public string Name => "role-per-tenant";

        public async Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        {
            await roles.AddAsync(Role.Create($"role-{(Guid)tenantId}").Value, cancellationToken);

            return seederSwitch.Fail || seederSwitch.FailForTenant == (Guid)tenantId
                ? Result.Failure(Error.Failure("Test.TenantSeed", "tenant seeder failed"))
                : Result.Success();
        }
    }
}
