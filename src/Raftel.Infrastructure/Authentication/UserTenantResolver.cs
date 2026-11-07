using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Authentication;

internal sealed class UserTenantResolver<TDbContext>(TDbContext dbContext) : IUserTenantResolver
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task<Guid?> ResolveAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        // The lookup must not depend on the tenant (or the X-Tenant-Id header) of the current request: the
        // user's own tenant is the source of truth. Soft-deleted records are included on purpose so a deleted
        // tenant user is never mistaken for a host user.
        return dbContext.Set<User>()
            .IgnoreQueryFilters()
            .Where(user => user.IdentityUserId == identityUserId)
            .Select(user => EF.Property<Guid?>(user, ShadowPropertyNames.TenantId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
