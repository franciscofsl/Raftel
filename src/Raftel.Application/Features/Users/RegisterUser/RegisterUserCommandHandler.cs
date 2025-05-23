using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;

namespace Raftel.Application.Features.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IAuthenticationService authenticationService,
    IUsersRepository usersRepository) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> HandleAsync(RegisterUserCommand request, CancellationToken token = default)
    {
        if (await usersRepository.EmailIsUniqueAsync(request.Email, token) == false)
        {
            return Result.Failure(UserErrors.DuplicatedEmail);
        }

        var user = User.Create(request.Email, request.Email, request.Email);
        var result = await authenticationService.RegisterAsync(user, request.Password, token);
        if (result.IsFailure)
        {
            return result;
        }

        user.BindTo(result.Value);
        await usersRepository.AddAsync(user, token);
        return result;
    }
}