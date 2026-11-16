namespace Raftel.Api.Server.Security;

/// <summary>
/// Reverse proxies trusted to set <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c>.
/// </summary>
/// <remarks>
/// Behind a load balancer, the connection IP is the proxy's. Without listing the proxy here, every client shares
/// one rate limit partition. Headers from any address not listed (other than loopback) are ignored, so a client
/// cannot evade the limits by sending a forged <c>X-Forwarded-For</c>.
/// </remarks>
public sealed class RaftelForwardedHeadersOptions
{
    /// <summary>IP addresses of trusted proxies, for example <c>10.0.0.10</c>. Loopback is always trusted.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Trusted networks in CIDR notation, for example <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; set; } = [];

    /// <summary>Maximum number of proxy hops processed in <c>X-Forwarded-For</c>. Defaults to 1.</summary>
    public int ForwardLimit { get; set; } = 1;
}
