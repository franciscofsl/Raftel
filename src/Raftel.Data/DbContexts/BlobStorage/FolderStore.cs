using Microsoft.EntityFrameworkCore;
using Raftel.Core.Storage;

namespace Raftel.Data.DbContexts.BlobStorage;

public class FolderStore : IFolderStore
{
    private readonly BlobDbContext _dbContext;

    public FolderStore(BlobDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Folder folder)
    {
        await _dbContext.Folders.AddAsync(folder);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Folder> GetAsync(Guid? folderId)
    {
        if (folderId == null)
        {
            return null;
        }

        return await _dbContext.Folders
            .Include(f => f.SubFolders)
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.Id == folderId);
    }

    public async Task<IReadOnlyList<FolderInfo>> AtLevelAsync(Guid? parentFolderId)
    {
        return await _dbContext.Folders
            // .Where(f => f. == parentFolderId)
            .Select(_ => new FolderInfo(_.Id, _.Name))
            .ToListAsync();
    }
}