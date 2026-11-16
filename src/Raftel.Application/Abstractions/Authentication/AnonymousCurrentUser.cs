namespace Raftel.Application.Abstractions.Authentication;

internal sealed class AnonymousCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? UserName => null;

    public IEnumerable<string> Roles => [];

    public bool HasPermission(string permission) => false;
}
