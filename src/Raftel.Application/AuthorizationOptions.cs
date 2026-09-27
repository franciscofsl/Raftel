namespace Raftel.Application;

/// <summary>
/// Configures how the permission-authorization pipeline reports failures.
/// </summary>
public sealed class AuthorizationOptions
{
    /// <summary>
    /// Whether a <c>Forbidden</c> error message enumerates the specific permissions the caller was
    /// missing. Defaults to <see langword="false"/>: enumerating missing permissions is useful in
    /// development but filters the permission model to a caller who shouldn't see it, so a consumer
    /// must opt in explicitly (e.g. via <see cref="RaftelApplicationBuilder.ConfigureAuthorization"/>
    /// gated on its own environment check).
    /// </summary>
    public bool IncludeMissingPermissionsInError { get; set; }
}
