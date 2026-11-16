namespace Raftel.Application.Authorization;

/// <summary>
/// Exposes the permission names declared in classes marked with <see cref="PermissionSetAttribute"/>.
/// </summary>
public interface IPermissionSetProvider
{
    /// <summary>
    /// Gets every declared permission name, without duplicates.
    /// </summary>
    IReadOnlyCollection<string> PermissionNames { get; }
}
