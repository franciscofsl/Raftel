using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Features.Tenants.GetTenant;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Queries;

public sealed class GetTenantQueryHandlerTests
{
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly IEntityVersionReader _versionReader = Substitute.For<IEntityVersionReader>();
    private readonly GetTenantQueryHandler _handler;

    public GetTenantQueryHandlerTests()
    {
        _handler = new GetTenantQueryHandler(_tenantsRepository, _versionReader);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTenant_WhenTenantExists()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create("Test Tenant", "TEST", "Test Description");
        var query = new GetTenantQuery(tenantId);

        _tenantsRepository.GetByIdAsync(new TenantId(tenantId), Arg.Any<CancellationToken>())
            .Returns(tenant.Value);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Test Tenant");
        result.Value.Code.ShouldBe("TEST");
        result.Value.Description.ShouldBe("Test Description");
    }

    [Fact]
    public async Task HandleAsync_Should_ExposeTheTenantVersion()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create("Test Tenant", "TEST", "Test Description").Value;
        _tenantsRepository.GetByIdAsync(new TenantId(tenantId), Arg.Any<CancellationToken>()).Returns(tenant);
        _versionReader.VersionOf(tenant).Returns("AAAAAAAAB9E=");

        var result = await _handler.HandleAsync(new GetTenantQuery(tenantId));

        result.Value.Version.ShouldBe("AAAAAAAAB9E=");
        result.Value.ShouldBeAssignableTo<IVersionedResult>();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantId = Guid.NewGuid();
        var query = new GetTenantQuery(tenantId);

        _tenantsRepository.GetByIdAsync(new TenantId(tenantId), Arg.Any<CancellationToken>())
            .Returns((Tenant)null);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("Tenant.NotFound");
    }
} 