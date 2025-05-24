using Microsoft.AspNetCore.Builder;

namespace Raftel.Infrastructure.Multitenancy.Middleware;

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
} 