using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Authorization;

public static class RoleErrors
{
    public static Error PermissionAlreadyExists => new("Role.PermissionAlreadyExists", "Permission already exists in this role");
    public static Error PermissionNotFound => new("Role.PermissionNotFound", "Permission not found in this role");
    public static Error InvalidName => new("Role.InvalidName", "Role name cannot be null or empty"); 
    public static Error InvalidPermissionName => new("Role.InvalidPermissionName", "Permission name cannot be null or empty");
} 