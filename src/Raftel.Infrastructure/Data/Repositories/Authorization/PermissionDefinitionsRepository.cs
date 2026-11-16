using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Infrastructure.Data.Repositories.Authorization;

internal sealed class PermissionDefinitionsRepository<TDbContext>(TDbContext dbContext)
    : EfRepository<TDbContext, PermissionDefinition, PermissionDefinitionId>(dbContext), IPermissionDefinitionsRepository
    where TDbContext : RaftelDbContext<TDbContext>
{
}
