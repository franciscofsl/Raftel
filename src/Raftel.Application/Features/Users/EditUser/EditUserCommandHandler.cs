using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Application.Features.Users.EditUser;

internal sealed class EditUserCommandHandler(
    IAuthenticationService authenticationService,
    IUsersRepository usersRepository) : ICommandHandler<EditUserCommand>
{
    public async Task<Result> HandleAsync(EditUserCommand request, CancellationToken token = default)
    {
        var userId = new UserId(request.UserId);
        var user = await usersRepository.GetByIdAsync(userId, token);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        var emailChanged = (string)user.Email != request.Email;
        if (emailChanged && await usersRepository.EmailIsUniqueAsync(request.Email, token) == false)
        {
            return Result.Failure(UserErrors.DuplicatedEmail);
        }

        if (emailChanged)
        {
            var emailResult = await authenticationService.UpdateEmailAsync(user, request.Email, token);
            if (emailResult.IsFailure)
            {
                return emailResult;
            }
        }

        user.Update(request.Name, request.Surname, request.Email);
        usersRepository.Update(user);
        return Result.Success();
    }
}
