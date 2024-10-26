using Raftel.Application.Contracts.Storage;
using Raftel.Application.Cqrs.Queries;

namespace Raftel.Application.BlobStorage.Queries.GetFolders;

public record GetFoldersQuery(Guid? ParentFolderId) : IQuery<List<FolderDto>>;