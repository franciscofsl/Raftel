using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Infrastructure.Multitenancy.Middleware;

internal class TenantMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private const string TenantHeaderName = "X-Tenant-Id";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant,
        ITenantsRepository tenantsRepository)
    {
        var tenantId = GetTenantIdFromRequest(context);

        if (!tenantId.HasValue)
        {
            await next(context);
            return;
        }

        if (!await TenantExistsAsync(tenantId.Value, tenantsRepository, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        using var scope = currentTenant.Change(tenantId.Value);
        await next(context);
    }

    private async Task<bool> TenantExistsAsync(Guid tenantId, ITenantsRepository tenantsRepository,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"tenant:{tenantId}";
        if (cache.TryGetValue<bool>(cacheKey, out _))
        {
            return true;
        }

        var tenant = await tenantsRepository.GetByIdAsync(new TenantId(tenantId), cancellationToken);
        if (tenant is null)
        {
            return false;
        }

        cache.Set(cacheKey, true, CacheExpiry);
        return true;
    }

    private static Guid? GetTenantIdFromRequest(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var headerValue))
        {
            return null;
        }

        var tenantIdString = headerValue.FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantIdString) && Guid.TryParse(tenantIdString, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }
}