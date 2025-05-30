﻿using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Domain.Features.Users;

public interface IUsersRepository : IRepository<User, UserId>
{
    Task<bool> EmailIsUniqueAsync(string email, CancellationToken token);
}