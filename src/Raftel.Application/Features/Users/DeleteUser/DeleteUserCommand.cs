using Raftel.Application.Authorization;
using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.DeleteUser;

[RequiresPermission(UsersPermissions.Delete)]
public sealed record DeleteUserCommand(Guid UserId) : ICommand;
