using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Users.LogInUser;
public sealed record LogInUserQuery(string Email, string Password) : IQuery<LogInUserResponse>;