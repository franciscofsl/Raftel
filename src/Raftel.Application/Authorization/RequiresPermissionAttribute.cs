﻿namespace Raftel.Application.Authorization;

/// <summary>
/// Specifies that the command or query requires a specific permission to be executed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequiresPermissionAttribute : Attribute
{
    /// <summary>
    /// Gets the permission required to execute the command or query.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RequiresPermissionAttribute"/> with the specified permission.
    /// </summary>
    /// <param name="permission">The permission required to execute the command or query.</param>
    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
