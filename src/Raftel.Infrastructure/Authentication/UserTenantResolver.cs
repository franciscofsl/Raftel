using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Authentication;

internal sealed class UserTenantResolver<TDbContext>(TDbContext dbContext) : IUserTenantResolver
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task<Guid?> ResolveAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<User>()
            .IgnoreQueryFilters()
            .Where(user => user.IdentityUserId == identityUserId)
            .Select(user => EF.Property<Guid?>(user, ShadowPropertyNames.TenantId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
