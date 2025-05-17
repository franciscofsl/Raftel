using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Users;

public static class UserErrors
{
    public static Error DuplicatedEmail => new("User.DuplicatedEmail", "User with this email already exists");
}