namespace Raftel.Core.Storage;

public interface IFolderStore
{
    Task AddAsync(Folder any);
}