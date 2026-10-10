using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data;

public abstract class TransactionTestsBase : InfrastructureTestBase
{
    protected TransactionTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BeginTransactionAsync_ThenCommit_PersistsData_AndClearsActiveTransaction()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var pirate = Pirate.Normal("Luffy", 150_000_000);

            unitOfWork.HasActiveTransaction.ShouldBeFalse();

            var transaction = await unitOfWork.BeginTransactionAsync();
            unitOfWork.HasActiveTransaction.ShouldBeTrue();

            dbContext.Add(pirate);
            await unitOfWork.CommitAsync();
            await transaction.CommitAsync();

            unitOfWork.HasActiveTransaction.ShouldBeFalse();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Luffy");

            loaded.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_ThenRollback_DoesNotPersistData()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var pirate = Pirate.Normal("Zoro", 120_000_000);

            var transaction = await unitOfWork.BeginTransactionAsync();

            dbContext.Add(pirate);
            await unitOfWork.CommitAsync();
            await transaction.RollbackAsync();

            unitOfWork.HasActiveTransaction.ShouldBeFalse();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Zoro");

            loaded.ShouldBeNull();
        });
    }

    [Fact]
    public async Task DisposingWithoutCommitOrRollback_RollsBack()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var pirate = Pirate.Normal("Sanji", 100_000_000);

            await using (var transaction = await unitOfWork.BeginTransactionAsync())
            {
                dbContext.Add(pirate);
                await unitOfWork.CommitAsync();
            }

            unitOfWork.HasActiveTransaction.ShouldBeFalse();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Sanji");

            loaded.ShouldBeNull();
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenAlreadyActive_OpensNoSecondPhysicalTransaction_AndNestedCommitIsNoOp()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var pirate = Pirate.Normal("Nami", 60_000_000);

            var root = await unitOfWork.BeginTransactionAsync();
            var physicalTransaction = dbContext.Database.CurrentTransaction;

            var nested = await unitOfWork.BeginTransactionAsync();

            dbContext.Database.CurrentTransaction.ShouldBeSameAs(physicalTransaction);

            await nested.CommitAsync();

            unitOfWork.HasActiveTransaction.ShouldBeTrue();

            dbContext.Add(pirate);
            await unitOfWork.CommitAsync();
            await root.CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Nami");

            loaded.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task NestedRollback_MarksRootRollbackOnly_AndRootCommitThrows()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var pirate = Pirate.Normal("Usopp", 30_000_000);

            var root = await unitOfWork.BeginTransactionAsync();
            var nested = await unitOfWork.BeginTransactionAsync();

            dbContext.Add(pirate);
            await unitOfWork.CommitAsync();

            await nested.RollbackAsync();

            await Should.ThrowAsync<InvalidOperationException>(() => root.CommitAsync());

            unitOfWork.HasActiveTransaction.ShouldBeFalse();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = await dbContext.Set<Pirate>().FirstOrDefaultAsync(p => p.Name == "Usopp");

            loaded.ShouldBeNull();
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_WithoutConfiguredIsolationLevel_UsesReadCommitted()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            dbContext.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel
                .ShouldBe(IsolationLevel.ReadCommitted);
        });
    }
}

public abstract class ConfiguredIsolationLevelTransactionTestsBase : InfrastructureTestBase
{
    protected ConfiguredIsolationLevelTransactionTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton(new TransactionOptions { IsolationLevel = IsolationLevel.Serializable });
    }

    [Fact]
    public async Task BeginTransactionAsync_WithConfiguredIsolationLevel_UsesIt()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            dbContext.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel
                .ShouldBe(IsolationLevel.Serializable);
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerTransactionTests : TransactionTestsBase
{
    public SqlServerTransactionTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlTransactionTests : TransactionTestsBase
{
    public PostgreSqlTransactionTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerConfiguredIsolationLevelTransactionTests : ConfiguredIsolationLevelTransactionTestsBase
{
    public SqlServerConfiguredIsolationLevelTransactionTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlConfiguredIsolationLevelTransactionTests : ConfiguredIsolationLevelTransactionTestsBase
{
    public PostgreSqlConfiguredIsolationLevelTransactionTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
