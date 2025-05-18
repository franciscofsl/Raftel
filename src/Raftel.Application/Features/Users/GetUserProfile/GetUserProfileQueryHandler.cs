using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Users.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(ICurrentUser currentUser)
    : IQueryHandler<GetUserProfileQuery, GetUserProfileResponse>
{
    public Task<Result<GetUserProfileResponse>> HandleAsync(GetUserProfileQuery request,
        CancellationToken token = default)
    {
        return Task.FromResult<Result<GetUserProfileResponse>>(new GetUserProfileResponse
        {
            UserName = currentUser.UserName,
            Roles = currentUser.Roles,
            IsAuthenticated = currentUser.IsAuthenticated,
            UserId = currentUser.UserId,
        });
    }
}