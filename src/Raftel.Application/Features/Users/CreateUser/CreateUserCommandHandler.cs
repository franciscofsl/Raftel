using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;

namespace Raftel.Application.Features.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IAuthenticationService authenticationService,
    IUsersRepository usersRepository) : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(CreateUserCommand request, CancellationToken token = default)
    {
        if (await usersRepository.EmailIsUniqueAsync(request.Email, token) == false)
        {
            return Result.Failure(UserErrors.DuplicatedEmail);
        }

        var user = User.Create(request.Email, request.Name, request.Surname);
        var result = await authenticationService.RegisterAsync(user, request.Password, token);
        if (result.IsFailure)
        {
            return result;
        }

        user.BindTo(result.Value);
        await usersRepository.AddAsync(user, token);
        return Result.Success();
    }
}
