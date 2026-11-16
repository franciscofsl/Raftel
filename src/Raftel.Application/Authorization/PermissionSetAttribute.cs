namespace Raftel.Application.Authorization;

/// <summary>
/// Marks a class whose public <c>const string</c> fields are permission names that the seeding
/// pipeline must keep in the permission catalog.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PermissionSetAttribute : Attribute
{
}
