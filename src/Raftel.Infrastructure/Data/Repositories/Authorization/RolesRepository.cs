using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Infrastructure.Data.Repositories.Authorization;

internal sealed class RolesRepository<TDbContext>(TDbContext dbContext)
    : EfRepository<TDbContext, Role, RoleId>(dbContext), IRolesRepository
    where TDbContext : RaftelDbContext<TDbContext>
{
}