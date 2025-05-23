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
        var user = User.Create(request.Email, request.Email, request.Email, string.Empty);

        if (await usersRepository.EmailIsUniqueAsync(request.Email, token) == false)
        {
            return Result.Failure(UserErrors.DuplicatedEmail);
        }

        var result = await authenticationService.RegisterAsync(user, request.Password, token);
        if (result.IsSuccess)
        {
            user = User.Create(request.Email, request.Email, request.Email, result.Value);
            await usersRepository.AddAsync(user, token);
        }

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}