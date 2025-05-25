using System.Collections;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Authorization;

internal class PermissionCollection : IEnumerable<Permission>
{
    private readonly List<Permission> _permissions = new();

    internal Result Add(string permissionName, string? description = null)
    {
        if (Has(permissionName))
        {
            return Result.Failure(RoleErrors.PermissionAlreadyExists);
        }

        var permissionResult = Permission.Create(permissionName, description);
        if (permissionResult.IsFailure)
        {
            return Result.Failure(permissionResult.Error);
        }

        _permissions.Add(permissionResult.Value);
        return Result.Success();
    }

    internal Result Remove(string permissionName)

        var permission = _permissions.FirstOrDefault(p => 
            p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        _permissions.Remove(permission);
        return Result.Success();
    }
    internal bool Has(string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return false;
        }

        return _permissions.Any(p => 
            p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }
    internal void Clear() => _permissions.Clear();
    public IEnumerator<Permission> GetEnumerator() => _permissions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
} 