using Microsoft.AspNetCore.Builder;

namespace Raftel.Api.Server.Middlewares;

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder) =>
        builder.UseMiddleware<CorrelationIdMiddleware>();
}
