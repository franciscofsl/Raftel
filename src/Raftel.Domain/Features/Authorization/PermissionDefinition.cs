using System.Diagnostics.CodeAnalysis;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Domain.Features.Authorization;

/// <summary>
/// An entry of the permission catalog: a permission name known to the application's code,
/// independent of any role that holds it.
/// </summary>
public sealed class PermissionDefinition : AggregateRoot<PermissionDefinitionId>
{
    [ExcludeFromCodeCoverage]
    private PermissionDefinition()
    {
    }

    private PermissionDefinition(string name, string? description) : base(PermissionDefinitionId.New())
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsObsolete { get; private set; }

    public static Result<PermissionDefinition> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<PermissionDefinition>(RoleErrors.InvalidPermissionName);
        }

        return Result.Success(new PermissionDefinition(name, description));
    }

    /// <summary>
    /// Flags the permission as no longer declared in code. Returns <see langword="true"/> when the
    /// flag changed, so callers can report only newly obsolete permissions.
    /// </summary>
    public bool MarkObsolete()
    {
        if (IsObsolete)
        {
            return false;
        }

        IsObsolete = true;
        return true;
    }

    /// <summary>
    /// Clears the obsolete flag because the permission is declared in code again.
    /// </summary>
    public bool Reinstate()
    {
        if (!IsObsolete)
        {
            return false;
        }

        IsObsolete = false;
        return true;
    }
}
