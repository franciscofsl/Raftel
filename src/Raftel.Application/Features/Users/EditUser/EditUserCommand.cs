using Raftel.Application.Authorization;
using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.EditUser;

[RequiresPermission(UsersPermissions.Edit)]
public sealed record EditUserCommand(Guid UserId, string Name, string Surname, string Email) : ICommand;
