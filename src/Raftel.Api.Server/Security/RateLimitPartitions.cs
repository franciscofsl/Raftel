using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Multitenancy;

namespace Raftel.Api.Server.Security;

internal static class RateLimitPartitions
{
    public static RateLimitPartition<string> Global(HttpContext context, RateLimitPolicyOptions options) =>
        RateLimitPartition.GetTokenBucketLimiter(ClientKey(context), _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = options.PermitLimit,
            TokensPerPeriod = options.PermitLimit,
            ReplenishmentPeriod = options.Window,
            QueueLimit = 0,
            AutoReplenishment = true
        });

    public static RateLimitPartition<string> Auth(HttpContext context, RateLimitPolicyOptions options) =>
        RateLimitPartition.GetFixedWindowLimiter(IpKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = options.Window,
            QueueLimit = 0,
            AutoReplenishment = true
        });

    public static RateLimitPartition<string> Write(HttpContext context, RateLimitPolicyOptions options) =>
        RateLimitPartition.GetSlidingWindowLimiter(ClientKey(context), _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = options.Window,
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        });

    /// <summary>
    /// Tenant plus authenticated user, or tenant plus client IP for anonymous callers. Including the tenant keeps
    /// a noisy tenant from consuming another tenant's quota.
    /// </summary>
    private static string ClientKey(HttpContext context)
    {
        var services = context.RequestServices;
        var user = services.GetService<ICurrentUser>();
        var subject = user is { IsAuthenticated: true, UserId: { } userId }
            ? $"user:{userId}"
            : IpKey(context);

        var tenantId = services.GetService<ICurrentTenant>()?.Id;
        return tenantId is null ? subject : $"tenant:{tenantId}:{subject}";
    }

    /// <summary>
    /// Credential endpoints are partitioned by IP only, so one client cannot spread attempts across many user
    /// names to stay under the limit.
    /// </summary>
    private static string IpKey(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
