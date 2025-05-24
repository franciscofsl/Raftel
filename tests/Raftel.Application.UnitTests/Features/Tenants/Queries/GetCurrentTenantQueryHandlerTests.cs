using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Features.Tenants.GetCurrentTenant;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Queries;

public sealed class GetCurrentTenantQueryHandlerTests
{
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly GetCurrentTenantQueryHandler _handler;

    public GetCurrentTenantQueryHandlerTests()
    {
        _handler = new GetCurrentTenantQueryHandler(_currentTenant);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnCurrentTenant_WhenTenantIsSet()
    {
        var tenantId = Guid.NewGuid();
        var query = new GetCurrentTenantQuery();

        _currentTenant.Id.Returns(tenantId);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TenantId.ShouldBe(tenantId);
        result.Value.IsMultiTenant.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNullTenant_WhenNoTenantIsSet()
    {
        var query = new GetCurrentTenantQuery();

        _currentTenant.Id.Returns((Guid?)null);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TenantId.ShouldBeNull();
        result.Value.IsMultiTenant.ShouldBeFalse();
    }
} 