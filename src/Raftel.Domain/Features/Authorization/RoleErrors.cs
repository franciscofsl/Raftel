using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Authorization;

public static class RoleErrors
{
    public static Error PermissionAlreadyExists => Error.Conflict("Role.PermissionAlreadyExists", "Permission already exists in this role");
    public static Error PermissionNotFound => Error.NotFound("Role.PermissionNotFound", "Permission not found in this role");
    public static Error InvalidName => Error.Validation("Role.InvalidName", "Role name cannot be null or empty");
    public static Error InvalidPermissionName => Error.Validation("Role.InvalidPermissionName", "Permission name cannot be null or empty");
} 