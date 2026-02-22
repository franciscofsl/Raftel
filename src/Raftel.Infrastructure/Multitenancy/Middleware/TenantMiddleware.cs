using Microsoft.AspNetCore.Http;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Infrastructure.Multitenancy.Middleware;

internal class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant,
        ITenantsRepository tenantsRepository)
    {
        var tenantId = GetTenantIdFromRequest(context);

        if (!tenantId.HasValue)
        {
            await next(context);
            return;
        }

        var tenant = await tenantsRepository.GetByIdAsync(new TenantId(tenantId.Value), context.RequestAborted);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        using var scope = currentTenant.Change(tenantId.Value);
        await next(context);
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