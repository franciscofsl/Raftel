using Microsoft.EntityFrameworkCore;
using Raftel.Core.Auditing;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityChangesReader(IDbContextFactory dbContextFactory) : IEntityChangesReader
{
    public async Task<EntityChangesLog> ForEntityAsync(string entityId)
    {
        var dbContext = dbContextFactory.Create<IDbContext>();
        
        var changes = await dbContext
            .Set<EntityChange>()
            .Where(_ => _.EntityId == entityId)
            .OrderBy(_ => _.OccurredOn)
            .ToListAsync();

        return new EntityChangesLog(changes);
    }
}