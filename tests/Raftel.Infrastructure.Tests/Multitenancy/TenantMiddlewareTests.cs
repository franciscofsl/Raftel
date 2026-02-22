using Microsoft.AspNetCore.Http;
using NSubstitute;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Infrastructure.Multitenancy.Middleware;

namespace Raftel.Infrastructure.Tests.Multitenancy;

public class TenantMiddlewareTests
{
    private readonly RequestDelegate _next = Substitute.For<RequestDelegate>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly TenantMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public TenantMiddlewareTests()
    {
        _middleware = new TenantMiddleware(_next);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_Should_SetTenantContext_WhenValidTenantHeaderExists()
    {
        var tenantId = Guid.NewGuid();
        var disposableScope = Substitute.For<IDisposable>();
        var tenant = Tenant.Create("Test", "TST", "Test tenant").Value;

        _httpContext.Request.Headers["X-Tenant-Id"] = tenantId.ToString();
        _tenantsRepository.GetByIdAsync(new TenantId(tenantId)).Returns(tenant);
        _currentTenant.Change(tenantId).Returns(disposableScope);

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.Received(1).Change(tenantId);
        await _next.Received(1).Invoke(_httpContext);
        disposableScope.Received(1).Dispose();
    }

    [Fact]
    public async Task InvokeAsync_Should_Return404_WhenTenantIdIsUnknown()
    {
        var unknownTenantId = Guid.NewGuid();

        _httpContext.Request.Headers["X-Tenant-Id"] = unknownTenantId.ToString();
        _tenantsRepository.GetByIdAsync(new TenantId(unknownTenantId)).Returns((Tenant)null);

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        _currentTenant.DidNotReceive().Change(Arg.Any<Guid>());
        await _next.DidNotReceive().Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenNoTenantHeaderExists()
    {
        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.DidNotReceive().Change(Arg.Any<Guid>());
        await _tenantsRepository.DidNotReceive().GetByIdAsync(Arg.Any<TenantId>());
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenTenantHeaderIsEmpty()
    {
        _httpContext.Request.Headers["X-Tenant-Id"] = string.Empty;

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.DidNotReceive().Change(Arg.Any<Guid>());
        await _tenantsRepository.DidNotReceive().GetByIdAsync(Arg.Any<TenantId>());
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenTenantHeaderIsInvalidGuid()
    {
        _httpContext.Request.Headers["X-Tenant-Id"] = "invalid-guid";

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.DidNotReceive().Change(Arg.Any<Guid>());
        await _tenantsRepository.DidNotReceive().GetByIdAsync(Arg.Any<TenantId>());
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenTenantHeaderIsNull()
    {
        _httpContext.Request.Headers["X-Tenant-Id"] = (string)null;

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.DidNotReceive().Change(Arg.Any<Guid>());
        await _tenantsRepository.DidNotReceive().GetByIdAsync(Arg.Any<TenantId>());
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_HandleMultipleTenantHeaders_UseFirst()
    {
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        var disposableScope = Substitute.For<IDisposable>();
        var tenant = Tenant.Create("Test", "TST", "Test tenant").Value;

        _httpContext.Request.Headers["X-Tenant-Id"] = new[] { tenantId1.ToString(), tenantId2.ToString() };
        _tenantsRepository.GetByIdAsync(new TenantId(tenantId1)).Returns(tenant);
        _currentTenant.Change(tenantId1).Returns(disposableScope);

        await _middleware.InvokeAsync(_httpContext, _currentTenant, _tenantsRepository);

        _currentTenant.Received(1).Change(tenantId1);
        _currentTenant.DidNotReceive().Change(tenantId2);
        await _next.Received(1).Invoke(_httpContext);
        disposableScope.Received(1).Dispose();
    }
} 