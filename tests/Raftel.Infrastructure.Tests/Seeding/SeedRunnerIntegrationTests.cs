using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Tests.Seeding;

public abstract class SeedRunnerIntegrationTestsBase : InfrastructureTestBase
{
    protected SeedRunnerIntegrationTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.RemoveAll<IDataSeeder>();
        services.Configure<SeedOptions>(options => options.FailFastOnError = false);
        services.AddScoped<IDataSeeder>(sp => new WritingSeeder(sp, "ok-1", 100));
        services.AddScoped<IDataSeeder>(sp => new WritingSeeder(sp, "ok-2", 200));
        services.AddScoped<IDataSeeder>(sp => new WritingSeeder(sp, "failing", 300, failWith: Error.Failure("Test.Boom", "boom")));
        services.AddScoped<IDataSeeder>(sp => new WritingSeeder(sp, "throwing", 400, throwWith: new InvalidOperationException("kaput")));
    }

    [Fact]
    public async Task RunAsync_ShouldPersistSuccessfulSeedersAndDiscardFailedOnes()
    {
        Result result = Result.Success();
        await ExecuteScopedAsync(async sp =>
        {
            result = await sp.GetRequiredService<ISeedRunner>().RunAsync();
        });

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("failing");
        result.Error.Message.ShouldContain("kaput");

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var names = await dbContext.Set<PermissionDefinition>().Select(p => p.Name).ToListAsync();
            names.OrderBy(n => n).ShouldBe(["ok-1", "ok-2"]);

            var history = await dbContext.Set<SeedHistory>().Select(h => h.Name).ToListAsync();
            history.OrderBy(n => n).ShouldBe(["ok-1", "ok-2"]);
        });
    }

    [Fact]
    public async Task RunAsync_ShouldUpsertHistoryOnSecondRun()
    {
        await ExecuteScopedAsync(async sp => await sp.GetRequiredService<ISeedRunner>().RunAsync());

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            (await dbContext.Set<SeedHistory>().CountAsync()).ShouldBe(2);
        });
    }

    [Fact]
    public async Task SeedHistoryStore_ShouldRecordAndReadBackByNameAndTenant()
    {
        var tenantId = Guid.NewGuid();
        await ExecuteScopedAsync(async sp =>
        {
            var store = sp.GetRequiredService<ISeedHistoryStore>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            await store.RecordAsync("seeder", null, 1, CancellationToken.None);
            await store.RecordAsync("seeder", tenantId, 3, CancellationToken.None);
            await unitOfWork.CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var store = sp.GetRequiredService<ISeedHistoryStore>();
            (await store.GetAppliedVersionAsync("seeder", null, CancellationToken.None)).ShouldBe(1);
            (await store.GetAppliedVersionAsync("seeder", tenantId, CancellationToken.None)).ShouldBe(3);
            (await store.GetAppliedVersionAsync("other", null, CancellationToken.None)).ShouldBeNull();
        });
    }

    private sealed class WritingSeeder(
        IServiceProvider services,
        string name,
        int order,
        Error? failWith = null,
        Exception? throwWith = null) : IDataSeeder
    {
        public int Order => order;
        public string Name => name;

        public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
        {
            var repository = services.GetRequiredService<IPermissionDefinitionsRepository>();
            await repository.AddAsync(PermissionDefinition.Create(name).Value, cancellationToken);
            await services.GetRequiredService<IUnitOfWork>().CommitAsync(cancellationToken);

            if (throwWith is not null)
            {
                throw throwWith;
            }

            return failWith is null ? Result.Success() : Result.Failure(failWith);
        }
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerSeedRunnerIntegrationTests : SeedRunnerIntegrationTestsBase
{
    public SqlServerSeedRunnerIntegrationTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlSeedRunnerIntegrationTests : SeedRunnerIntegrationTestsBase
{
    public PostgreSqlSeedRunnerIntegrationTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
