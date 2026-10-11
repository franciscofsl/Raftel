using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Concurrency;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Default <see cref="IEntityVersionReader"/>, based on the change tracker of the unit of work.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
internal sealed class EntityVersionReader<TDbContext>(TDbContext dbContext) : IEntityVersionReader
    where TDbContext : RaftelDbContext<TDbContext>
{
    public string VersionOf(object entity)
    {
        var entry = dbContext.Entry(entity);

        if (entry.State == EntityState.Detached || entry.Metadata.FindProperty(ShadowPropertyNames.Version) is null)
        {
            return null;
        }

        return VersionCodec.Encode(entry.Property(ShadowPropertyNames.Version).CurrentValue);
    }
}
