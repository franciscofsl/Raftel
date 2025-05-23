using System.Security.Claims;
using Raftel.Application.Features.Users.LogInUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;

namespace Raftel.Application.Abstractions.Authentication;

public interface IAuthenticationService
{
    Task<Result<string>> RegisterAsync(User user, string password, CancellationToken cancellationToken = default);
    
    Task<Result<LogInResult>> LogInAsync(string email, string password, CancellationToken cancellationToken = default);
}