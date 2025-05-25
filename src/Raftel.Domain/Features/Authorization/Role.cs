using System.Diagnostics.CodeAnalysis;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Domain.Features.Authorization;

public sealed class Role : AggregateRoot<RoleId>
{
    private readonly PermissionCollection _permissions = new();

    [ExcludeFromCodeCoverage]
    private Role()
    {
    }

    private Role(string name, string? description = null) : base(RoleId.New())
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; set; }

    public static Result<Role> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(RoleErrors.InvalidName);
        }

        return Result.Success(new Role(name, description));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(RoleErrors.InvalidName);
        }

        Name = name;
        return Result.Success();
    }

    public Result AddPermission(string permissionName, string? description = null)
    {
        return _permissions.Add(permissionName, description);
    }
} 