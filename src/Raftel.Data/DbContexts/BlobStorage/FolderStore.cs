using Raftel.Core.Storage;

namespace Raftel.Data.DbContexts.BlobStorage;

public class FolderStore : IFolderStore
{
    public Task AddAsync(Folder any)
    {
        throw new NotImplementedException();
    }

    public Task<Folder> GetAsync(Guid? folderId)
    {
        throw new NotImplementedException();
    }
}