namespace Raftel.Api.Server.Security;

/// <summary>
/// Limits for the three named policies in <see cref="RaftelRateLimitPolicies"/>.
/// </summary>
public sealed class RaftelRateLimitOptions
{
    /// <summary>Whether <c>UseRaftelRateLimiting</c> adds the rate limiter. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <see cref="RaftelRateLimitPolicies.Global"/>: token bucket applied to every endpoint,
    /// partitioned by tenant and user (or client IP when anonymous). Defaults to 100 requests per minute.
    /// </summary>
    public RateLimitPolicyOptions Global { get; set; } = new() { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) };

    /// <summary>
    /// <see cref="RaftelRateLimitPolicies.Auth"/>: fixed window applied to credential endpoints
    /// (<c>/connect/token</c>, user registration), partitioned by client IP. Defaults to 5 attempts per 5 minutes.
    /// </summary>
    public RateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(5) };

    /// <summary>
    /// <see cref="RaftelRateLimitPolicies.Write"/>: sliding window applied to every AutoEndpoints command,
    /// partitioned by tenant and user (or client IP when anonymous). Defaults to 30 requests per minute.
    /// </summary>
    public RateLimitPolicyOptions Write { get; set; } = new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) };
}
