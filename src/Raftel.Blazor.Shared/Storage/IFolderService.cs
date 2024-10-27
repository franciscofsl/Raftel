using Raftel.Application.Contracts.Storage;
using Raftel.Blazor.Shared.Grpc;

namespace Raftel.Blazor.Shared.Storage;

public interface IFolderService : ICrudService<FolderDto, CreateFolderDto, FolderDto, FolderFilterDto>
{
}