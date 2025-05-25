using System.Diagnostics.CodeAnalysis;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Domain.Features.Authorization;

public sealed class Permission : Entity<PermissionId>
{
    [ExcludeFromCodeCoverage]
    private Permission()
    {
    }

    private Permission(string name, string? description = null) : base(PermissionId.New())
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; set; }

    public static Result<Permission> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Permission>(RoleErrors.InvalidPermissionName);
        }

        return Result.Success(new Permission(name, description));
    }
}