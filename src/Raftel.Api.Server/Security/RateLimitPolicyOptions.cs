namespace Raftel.Api.Server.Security;

/// <summary>Number of requests allowed per partition within a time window.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Requests allowed per <see cref="Window"/>. Must be greater than zero.</summary>
    public int PermitLimit { get; set; }

    /// <summary>Length of the window. Must be greater than zero.</summary>
    public TimeSpan Window { get; set; }
}
