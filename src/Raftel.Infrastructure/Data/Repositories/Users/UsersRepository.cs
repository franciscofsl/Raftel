﻿using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Infrastructure.Data.Repositories.Users;

internal sealed class UsersRepository<TDbContext>(TDbContext dbContext)
    : EfRepository<TDbContext, User, UserId>(dbContext), IUsersRepository
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task<bool> EmailIsUniqueAsync(string email, CancellationToken token)
    {
        return Task.FromResult(true);
    }
}