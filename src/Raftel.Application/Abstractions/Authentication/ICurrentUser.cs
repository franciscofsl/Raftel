namespace Raftel.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? UserName { get; }
    IEnumerable<string> Roles { get; }
}
