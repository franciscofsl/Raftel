namespace Raftel.Application.Authorization;

/// <summary>
/// Declares that a command must not be applied blindly: when ETag support is enabled, its endpoint
/// rejects a request that does not carry the version it read in an <c>If-Match</c> header
/// (<c>428 Precondition Required</c>). A wildcard <c>If-Match: *</c> does not satisfy it.
/// </summary>
/// <remarks>
/// Only put it on commands that modify a version-tracked aggregate; on any other command there is
/// nothing to compare the version against.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public sealed class RequiresExpectedVersionAttribute : Attribute
{
}
