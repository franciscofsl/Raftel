using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.AssignRoleToUser;

[RequiresPermission("users.manage")]
[RequiresPermission("roles.assign")]
public record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : ICommand;