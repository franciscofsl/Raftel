namespace Raftel.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    IEnumerable<string> Roles { get; }
}
