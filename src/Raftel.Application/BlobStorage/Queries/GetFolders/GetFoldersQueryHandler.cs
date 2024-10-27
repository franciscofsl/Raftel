using Raftel.Application.Contracts.Storage;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Storage;

namespace Raftel.Application.BlobStorage.Queries.GetFolders;

public class GetFoldersQueryHandler(IFolderStore folderStore) : IQueryHandler<GetFoldersQuery, List<FolderDto>>
{
    public async Task<List<FolderDto>> Handle(GetFoldersQuery query, CancellationToken cancellationToken = default)
    {
        var folders = await folderStore.AtLevelAsync(query.ParentFolderId);

        return folders
            .Select(_ => new FolderDto
            {
                Id = _.Id,
                Name = _.Name
            })
            .ToList();
    }
}