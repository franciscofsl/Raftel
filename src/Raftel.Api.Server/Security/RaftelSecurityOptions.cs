namespace Raftel.Api.Server.Security;

/// <summary>
/// Rate limiting, CORS, security headers and forwarded headers settings applied by
/// <see cref="RaftelSecurityServiceCollectionExtensions.AddRaftelSecurity"/>.
/// </summary>
/// <remarks>
/// Bound from the <see cref="SectionName"/> configuration section, then from the optional configure callback.
/// The options are validated at startup: an invalid combination stops the host before it serves traffic.
/// </remarks>
public sealed class RaftelSecurityOptions
{
    /// <summary>Configuration section the options are bound from.</summary>
    public const string SectionName = "Raftel:Security";

    /// <summary>Rate limiting policies. Enabled by default.</summary>
    public RaftelRateLimitOptions RateLimiting { get; set; } = new();

    /// <summary>CORS policy. Disabled until at least one origin is configured.</summary>
    public RaftelCorsOptions Cors { get; set; } = new();

    /// <summary>Security response headers.</summary>
    public SecurityHeadersOptions Headers { get; set; } = new();

    /// <summary>Trusted reverse proxies whose <c>X-Forwarded-*</c> headers are honored.</summary>
    public RaftelForwardedHeadersOptions ForwardedHeaders { get; set; } = new();
}
