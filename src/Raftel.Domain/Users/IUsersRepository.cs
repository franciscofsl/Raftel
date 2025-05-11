using Raftel.Domain.Abstractions;
using Raftel.Domain.Users.ValueObjects;

namespace Raftel.Domain.Users;

public interface IUsersRepository : IRepository<User, UserId>
{
    Task<bool> EmailIsUniqueAsync(string email, CancellationToken token);
}