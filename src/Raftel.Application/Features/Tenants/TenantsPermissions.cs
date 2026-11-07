namespace Raftel.Application.Features.Tenants;

[PermissionSet]
public static class TenantsPermissions
{
    public const string Management = "Tenants.Management";
    public const string View = "Tenants.View";

    /// <summary>
    /// Lets a host user (one that belongs to no tenant) act within another tenant by sending the
    /// <c>X-Tenant-Id</c> header. Users that belong to a tenant are always pinned to it.
    /// </summary>
    public const string Switch = "Tenants.Switch";
}