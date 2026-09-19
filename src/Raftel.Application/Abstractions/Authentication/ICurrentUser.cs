namespace Raftel.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? UserName { get; }
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Returns whether the current user holds <paramref name="permission"/>, without throwing.
    /// An unauthenticated user always returns <see langword="false"/>.
    /// </summary>
    bool HasPermission(string permission);

    [Obsolete("Throws for business-logic flow. Use HasPermission instead. Will be removed in a future version.")]
    void EnsureHasPermission(string permission);
}
