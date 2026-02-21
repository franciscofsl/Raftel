using Raftel.Application.Authorization;
using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.CreateUser;

[RequiresPermission(UsersPermissions.Create)]
public sealed record CreateUserCommand(string Email, string Name, string Surname, string Password) : ICommand;
