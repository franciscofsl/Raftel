using System.Linq.Expressions;
using Raftel.Application.Features.Tenants.GetAllTenants;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Specifications;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Queries;

public sealed class GetAllTenantsQueryHandlerTests
{
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly GetAllTenantsQueryHandler _handler;

    public GetAllTenantsQueryHandlerTests()
    {
        _handler = new GetAllTenantsQueryHandler(_tenantsRepository, new PaginationOptions());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnAllTenants_WhenTenantsExist()
    {
        var tenant1 = Tenant.Create("Tenant 1", "T1", "Description 1").Value;
        var tenant2 = Tenant.Create("Tenant 2", "T2", "Description 2").Value;
        var tenants = new PagedResult<Tenant>([tenant1, tenant2], page: 1, pageSize: 20, totalCount: 2);
        var query = new GetAllTenantsQuery(null, null, null);

        _tenantsRepository.ListPagedAsync(Arg.Any<PageRequest>(), Arg.Any<Expression<Func<Tenant, bool>>>(),
                Arg.Any<IReadOnlyList<SortSelector<Tenant>>>(), Arg.Any<CancellationToken>())
            .Returns(tenants);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items[0].Name.ShouldBe("Tenant 1");
        result.Value.Items[0].Code.ShouldBe("T1");
        result.Value.Items[0].Description.ShouldBe("Description 1");
        result.Value.Items[1].Name.ShouldBe("Tenant 2");
        result.Value.Items[1].Code.ShouldBe("T2");
        result.Value.Items[1].Description.ShouldBe("Description 2");
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmptyList_WhenNoTenantsExist()
    {
        var query = new GetAllTenantsQuery(null, null, null);
        var page = PageRequest.Create().Value;
        var tenants = PagedResult<Tenant>.Empty(page);

        _tenantsRepository.ListPagedAsync(Arg.Any<PageRequest>(), Arg.Any<Expression<Func<Tenant, bool>>>(),
                Arg.Any<IReadOnlyList<SortSelector<Tenant>>>(), Arg.Any<CancellationToken>())
            .Returns(tenants);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationFailure_WhenPageIsInvalid()
    {
        var query = new GetAllTenantsQuery(0, null, null);

        var result = await _handler.HandleAsync(query);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Page.Invalid");
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationFailure_WhenSortFieldIsUnknown()
    {
        var query = new GetAllTenantsQuery(null, null, "secretInternalColumn");

        var result = await _handler.HandleAsync(query);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Sort.UnknownField");
    }
}
