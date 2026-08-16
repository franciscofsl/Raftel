using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Users;

public static class UserErrors
{
    public static Error DuplicatedEmail => Error.Conflict("User.DuplicatedEmail", "User with this email already exists");
    public static Error RoleAlreadyAssigned => Error.Conflict("User.RoleAlreadyAssigned", "Role is already assigned to the user");
    public static Error NotFound => Error.NotFound("User.NotFound", "User was not found");
}