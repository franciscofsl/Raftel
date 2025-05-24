using Raftel.Application.Features.Tenants.GetAllTenants;
using Raftel.Domain.Features.Tenants;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Queries;

public sealed class GetAllTenantsQueryHandlerTests
{
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly GetAllTenantsQueryHandler _handler;

    public GetAllTenantsQueryHandlerTests()
    {
        _handler = new GetAllTenantsQueryHandler(_tenantsRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnAllTenants_WhenTenantsExist()
    {
        var tenant1 = Tenant.Create("Tenant 1", "T1", "Description 1");
        var tenant2 = Tenant.Create("Tenant 2", "T2", "Description 2");
        var tenants = new List<Tenant> { tenant1.Value, tenant2.Value };
        var query = new GetAllTenantsQuery();

        _tenantsRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(tenants);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("Tenant 1");
        result.Value[0].Code.ShouldBe("T1");
        result.Value[0].Description.ShouldBe("Description 1");
        result.Value[1].Name.ShouldBe("Tenant 2");
        result.Value[1].Code.ShouldBe("T2");
        result.Value[1].Description.ShouldBe("Description 2");
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmptyList_WhenNoTenantsExist()
    {
        var tenants = new List<Tenant>();
        var query = new GetAllTenantsQuery();

        _tenantsRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(tenants);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(0);
    }
} 