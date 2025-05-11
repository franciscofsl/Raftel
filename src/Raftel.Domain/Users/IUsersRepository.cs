using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Users;

public interface IUsersRepository : IRepository<User, UserId>
{
    Task<bool> EmailIsUniqueAsync(string email, CancellationToken token);
}