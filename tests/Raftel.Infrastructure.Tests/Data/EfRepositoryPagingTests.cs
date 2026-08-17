using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Specifications;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Data;

public abstract class EfRepositoryPagingTestsBase : InfrastructureTestBase
{
    protected EfRepositoryPagingTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ListPagedAsync_Should_Return_Correct_Page_And_TotalCount()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            foreach (var index in Enumerable.Range(1, 7))
            {
                await repository.AddAsync(Pirate.Normal($"Pirate {index:00}", (uint)(index * 1_000_000)));
            }

            await unitOfWork.CommitAsync();

            var sortMap = new SortMap<Pirate>().Allow("bounty", p => p.Bounty);
            var sort = sortMap.ResolveAll([new SortRequest("bounty")]).Value;
            var page = PageRequest.Create(page: 2, pageSize: 3).Value;

            var result = await repository.ListPagedAsync(page, sort: sort, cancellationToken: CancellationToken.None);

            result.TotalCount.ShouldBe(7);
            result.Items.Count.ShouldBe(3);
            result.Items.Select(p => (uint)p.Bounty).ShouldBe([4_000_000u, 5_000_000u, 6_000_000u]);
        });
    }

    [Fact]
    public async Task ListPagedAsync_Should_Return_Every_Item_Exactly_Once_Across_All_Pages()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();

            var created = Enumerable.Range(1, 11)
                .Select(index => Pirate.Normal($"Pirate {index:00}", (uint)(index * 1_000_000)))
                .ToList();

            foreach (var pirate in created)
            {
                await repository.AddAsync(pirate);
            }

            await unitOfWork.CommitAsync();

            const int pageSize = 3;
            var firstPage = PageRequest.Create(page: 1, pageSize: pageSize).Value;
            var firstResult =
                await repository.ListPagedAsync(firstPage, cancellationToken: CancellationToken.None);

            var seen = new List<Pirate>();
            for (var pageNumber = 1; pageNumber <= firstResult.TotalPages; pageNumber++)
            {
                var page = PageRequest.Create(page: pageNumber, pageSize: pageSize).Value;
                var result = await repository.ListPagedAsync(page, cancellationToken: CancellationToken.None);
                seen.AddRange(result.Items);
            }

            seen.Count.ShouldBe(created.Count);
            seen.Select(p => p.Id).Distinct().Count().ShouldBe(created.Count);
            seen.Select(p => p.Id).ShouldBe(created.Select(p => p.Id), ignoreOrder: true);
        });
    }

    [Fact]
    public async Task ListPagedAsync_Should_Apply_Tenant_Filter_To_Count_And_Page()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IPirateRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var tenantAId = Guid.NewGuid();
            var tenantBId = Guid.NewGuid();

            currentTenant.Change(tenantAId);
            await repository.AddAsync(Pirate.Normal("Luffy", 150_000_000));
            await unitOfWork.CommitAsync();

            currentTenant.Change(tenantBId);
            var zoro = Pirate.Normal("Zoro", 120_000_000);
            await repository.AddAsync(zoro);
            await unitOfWork.CommitAsync();

            var page = PageRequest.Create(page: 1, pageSize: 10).Value;
            var result = await repository.ListPagedAsync(page, cancellationToken: CancellationToken.None);

            result.TotalCount.ShouldBe(1);
            result.Items.ShouldContain(zoro);
        });
    }

    [Fact]
    public async Task ListPagedAsync_Should_Apply_SoftDelete_Filter_To_Count_And_Page()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IShipRepository>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var thousandSunny = Ship.Create("Thousand Sunny");
            var goingMerry = Ship.Create("Going Merry");
            await repository.AddAsync(thousandSunny);
            await repository.AddAsync(goingMerry);
            await unitOfWork.CommitAsync();

            repository.Remove(goingMerry);
            await unitOfWork.CommitAsync();

            var page = PageRequest.Create(page: 1, pageSize: 10).Value;
            var result = await repository.ListPagedAsync(page, cancellationToken: CancellationToken.None);

            result.TotalCount.ShouldBe(1);
            result.Items.ShouldContain(thousandSunny);

            using (dataFilter.Disable<ISoftDeleteFilter>())
            {
                var resultWithoutFilter =
                    await repository.ListPagedAsync(page, cancellationToken: CancellationToken.None);
                resultWithoutFilter.TotalCount.ShouldBe(2);
            }
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerEfRepositoryPagingTests : EfRepositoryPagingTestsBase
{
    public SqlServerEfRepositoryPagingTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlEfRepositoryPagingTests : EfRepositoryPagingTestsBase
{
    public PostgreSqlEfRepositoryPagingTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
