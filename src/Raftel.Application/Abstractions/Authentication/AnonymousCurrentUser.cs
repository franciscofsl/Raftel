namespace Raftel.Application.Abstractions.Authentication;

/// <summary>
/// Fallback <see cref="ICurrentUser"/> used when the host registers no user accessor (for example,
/// a worker or a test host without the Infrastructure layer). It always represents an anonymous
/// caller holding no permission, so permission-protected requests fail closed instead of running.
/// </summary>
internal sealed class AnonymousCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? UserName => null;

    public IEnumerable<string> Roles => [];

    public bool HasPermission(string permission) => false;
}
