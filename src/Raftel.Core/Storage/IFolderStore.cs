namespace Raftel.Core.Storage;

public interface IFolderStore
{
    Task AddAsync(Folder any);
    Task<Folder> GetAsync(Guid? folderId);
    Task<IReadOnlyList<FolderInfo>> AtLevelAsync(Guid? parentFolderId);
}