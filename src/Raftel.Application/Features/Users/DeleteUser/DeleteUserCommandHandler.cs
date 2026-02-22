using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Application.Features.Users.DeleteUser;

internal sealed class DeleteUserCommandHandler(
    IAuthenticationService authenticationService,
    IUsersRepository usersRepository) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> HandleAsync(DeleteUserCommand request, CancellationToken token = default)
    {
        var userId = new UserId(request.UserId);
        var user = await usersRepository.GetByIdAsync(userId, token);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        var deleteResult = await authenticationService.DeleteAsync(user, token);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        usersRepository.Remove(user);
        return Result.Success();
    }
}
