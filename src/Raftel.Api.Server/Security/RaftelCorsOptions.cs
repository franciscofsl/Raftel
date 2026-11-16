namespace Raftel.Api.Server.Security;

/// <summary>
/// Explicit CORS policy. No origin is allowed by default: an empty <see cref="AllowedOrigins"/> disables CORS.
/// </summary>
public sealed class RaftelCorsOptions
{
    internal static readonly string[] DefaultMethods = ["GET", "POST", "PUT", "DELETE", "PATCH"];
    internal static readonly string[] DefaultHeaders = ["Content-Type", "Authorization", "X-Correlation-Id"];
    internal static readonly string[] DefaultExposedHeaders = ["X-Correlation-Id", "X-Total-Count"];

    /// <summary>Origins allowed to call the API. Empty disables CORS. <c>"*"</c> allows any origin.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Allowed methods. Empty uses GET, POST, PUT, DELETE and PATCH.</summary>
    public string[] AllowedMethods { get; set; } = [];

    /// <summary>Allowed request headers. Empty uses Content-Type, Authorization and X-Correlation-Id.</summary>
    public string[] AllowedHeaders { get; set; } = [];

    /// <summary>
    /// Response headers the browser may read. Empty uses X-Correlation-Id and X-Total-Count,
    /// the headers the framework emits.
    /// </summary>
    public string[] ExposedHeaders { get; set; } = [];

    /// <summary>
    /// Whether credentials (cookies, authorization headers) are allowed. Cannot be combined with the
    /// <c>"*"</c> origin: that combination stops the host at startup.
    /// </summary>
    public bool AllowCredentials { get; set; }

    /// <summary>How long browsers may cache a preflight response. Defaults to one hour.</summary>
    public TimeSpan PreflightMaxAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// In the Development environment only, allows any origin and logs a warning.
    /// Ignored, with a warning, in any other environment.
    /// </summary>
    public bool AllowDevelopmentOrigins { get; set; }

    internal bool AllowsAnyOrigin => AllowedOrigins.Contains("*");

    internal string[] EffectiveMethods => AllowedMethods.Length > 0 ? AllowedMethods : DefaultMethods;

    internal string[] EffectiveHeaders => AllowedHeaders.Length > 0 ? AllowedHeaders : DefaultHeaders;

    internal string[] EffectiveExposedHeaders => ExposedHeaders.Length > 0 ? ExposedHeaders : DefaultExposedHeaders;
}
