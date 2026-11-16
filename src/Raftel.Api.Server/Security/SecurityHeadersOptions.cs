namespace Raftel.Api.Server.Security;

/// <summary>
/// Values written by <see cref="Middlewares.SecurityHeadersMiddleware"/>. Set a value to an empty string to omit
/// that header.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary><c>X-Content-Type-Options</c>. Defaults to <c>nosniff</c>.</summary>
    public string ContentTypeOptions { get; set; } = "nosniff";

    /// <summary><c>X-Frame-Options</c>. Defaults to <c>DENY</c>.</summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary><c>Referrer-Policy</c>. Defaults to <c>strict-origin-when-cross-origin</c>.</summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// <c>Content-Security-Policy</c> for API responses. Defaults to <c>default-src 'none'; frame-ancestors 'none'</c>,
    /// which suits a JSON API.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'none'; frame-ancestors 'none'";

    /// <summary>
    /// Path prefix of the API documentation UI (for example Swagger UI). Requests under it get
    /// <see cref="DocumentationContentSecurityPolicy"/> instead of <see cref="ContentSecurityPolicy"/>.
    /// Defaults to <c>/swagger</c>. Empty disables the override.
    /// </summary>
    public string DocumentationPath { get; set; } = "/swagger";

    /// <summary>Relaxed <c>Content-Security-Policy</c> that lets the documentation UI load its scripts and styles.</summary>
    public string DocumentationContentSecurityPolicy { get; set; } =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; frame-ancestors 'none'";

    /// <summary>
    /// <c>Strict-Transport-Security</c>, sent only on HTTPS requests outside the Development environment.
    /// Defaults to <c>max-age=31536000; includeSubDomains</c>.
    /// </summary>
    public string StrictTransportSecurity { get; set; } = "max-age=31536000; includeSubDomains";
}
