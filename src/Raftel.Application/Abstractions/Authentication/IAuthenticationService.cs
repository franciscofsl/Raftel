using Raftel.Domain.Abstractions;
using Raftel.Domain.Users;

namespace Raftel.Application.Abstractions.Authentication;

public interface IAuthenticationService
{
    Task<Result> RegisterAsync(User user, string password, CancellationToken cancellationToken = default);
}