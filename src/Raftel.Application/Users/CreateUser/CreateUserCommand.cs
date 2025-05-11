using Raftel.Application.Commands;

namespace Raftel.Application.Users.CreateUser;
public sealed record CreateUserCommand(string Name, string Surname, string Email, string Password) : ICommand;