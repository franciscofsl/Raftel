namespace Raftel.Application.Features.Users.GetUserProfile;

public sealed class GetUserProfileResponse
{
    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public IEnumerable<string> Roles { get; set; }
}