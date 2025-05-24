using Microsoft.AspNetCore.Http;
using Raftel.Application.Abstractions.Multitenancy;

namespace Raftel.Infrastructure.Multitenancy.Middleware;

internal class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
    {
        var tenantId = GetTenantIdFromRequest(context);

        if (tenantId.HasValue)
        {
            using var scope = currentTenant.Change(tenantId.Value);
            await next(context);
        }
        else
        {
            await next(context);
        }
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