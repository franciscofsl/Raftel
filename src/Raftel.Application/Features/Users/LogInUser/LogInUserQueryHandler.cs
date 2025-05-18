using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Users.LogInUser;

internal sealed class LogInUserQueryHandler(IAuthenticationService authenticationService)
    : IQueryHandler<LogInUserQuery, LogInUserResponse>
{
    public async Task<Result<LogInUserResponse>> HandleAsync(LogInUserQuery request, CancellationToken token = default)
    {
        var loginResult = await authenticationService.LogInAsync(request.Email, request.Password, token);

        return loginResult.IsSuccess
            ? new LogInUserResponse(loginResult.Value.ClaimsPrincipal, loginResult.Value.Scheme)
            : Result<LogInUserResponse>.Failure(loginResult.Error);
    }
}