namespace Raftel.Api.Server.Security;

/// <summary>Names of the rate limiting policies registered by <c>AddRaftelSecurity</c>.</summary>
public static class RaftelRateLimitPolicies
{
    /// <summary>Applied to every request as the global limiter.</summary>
    public const string Global = "raftel-global";

    /// <summary>Applied to credential endpoints such as <c>/connect/token</c>.</summary>
    public const string Auth = "raftel-auth";

    /// <summary>Applied to every AutoEndpoints command unless the command declares another policy.</summary>
    public const string Write = "raftel-write";
}
