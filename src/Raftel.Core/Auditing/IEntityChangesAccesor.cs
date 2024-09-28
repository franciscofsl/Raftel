namespace Raftel.Core.Auditing;

public interface IEntityChangesReader
{
    Task<EntityChangesLog> ForEntityAsync(string entityId);
}