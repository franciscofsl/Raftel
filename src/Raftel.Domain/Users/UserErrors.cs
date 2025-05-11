using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Users;

public static class UserErrors
{
    public static Error DuplicatedEmail => new("User.DuplicatedEmail", "User with this email already exists");
}