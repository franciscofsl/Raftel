using System.Collections;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Authorization;

internal class PermissionCollection : IEnumerable<Permission>
{
    private readonly List<Permission> _permissions = new();


    internal Result Add(Permission permission)
    {

        _permissions.Add(permission);
        return Result.Success();
    }

    public IEnumerator<Permission> GetEnumerator() => _permissions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
} 