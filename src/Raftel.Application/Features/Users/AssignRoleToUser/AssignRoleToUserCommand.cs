using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.AssignRoleToUser;

public record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : ICommand;