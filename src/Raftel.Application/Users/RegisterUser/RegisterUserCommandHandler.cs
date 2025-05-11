using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Users;

namespace Raftel.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IAuthenticationService authenticationService,
    IUsersRepository usersRepository) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> HandleAsync(RegisterUserCommand request, CancellationToken token = default)
    {
        var user = User.Create(request.Email, request.Name, request.Surname);

        if (await usersRepository.EmailIsUniqueAsync(request.Email, token) == false)
        {
            return Result.Failure(UserErrors.DuplicatedEmail);
        }

        var result = await authenticationService.RegisterAsync(user, request.Password, token);
        if (result.IsSuccess)
        {
            await usersRepository.AddAsync(user, token);
        }

        return result;
    }
}