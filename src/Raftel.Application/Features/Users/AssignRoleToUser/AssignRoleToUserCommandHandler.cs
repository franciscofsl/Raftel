using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Application.Features.Users.AssignRoleToUser;

internal sealed class AssignRoleToUserCommandHandler(
    IUsersRepository userRepository,
    IRolesRepository roleRepository,
    IAuthenticationService authenticationService)
    : ICommandHandler<AssignRoleToUserCommand>
{
    public async Task<Result> HandleAsync(AssignRoleToUserCommand request, CancellationToken token = default)
    {
        var userId = new UserId(request.UserId);
        var user = await userRepository.GetByIdAsync(userId, token);

        var roleId = new RoleId(request.RoleId);
        var role = await roleRepository.GetByIdAsync(roleId, token);

        var identityResult = await authenticationService.AssignRoleAsync(user, role, token);
        if (identityResult.IsFailure)
        {
            return identityResult;
        }

        var result = user.AssignRole(role);
        if (result.IsFailure)
        {
            return result;
        }

        userRepository.Update(user);
        return Result.Success();
    }
}