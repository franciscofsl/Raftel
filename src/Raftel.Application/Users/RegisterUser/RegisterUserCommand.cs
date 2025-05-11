using Raftel.Application.Commands;

namespace Raftel.Application.Users.RegisterUser;
public sealed record RegisterUserCommand(string Name, string Surname, string Email, string Password) : ICommand;