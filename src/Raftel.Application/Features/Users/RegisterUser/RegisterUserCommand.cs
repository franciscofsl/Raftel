using Raftel.Application.Commands;

namespace Raftel.Application.Features.Users.RegisterUser;
public sealed record RegisterUserCommand(string Email, string Password) : ICommand;