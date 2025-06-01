using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Users.GetUserProfile;

[RequiresPermission(UsersPermissions.View)]
public sealed record GetUserProfileQuery() : IQuery<GetUserProfileResponse>;