using System.ServiceModel;
using Raftel.Application.BlobStorage.Commands.CreateFolder;
using Raftel.Application.BlobStorage.Queries.GetFolders;
using Raftel.Application.Contracts.Storage;
using Raftel.Application.Cqrs.Commands;
using Raftel.Application.Cqrs.Queries;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Storage;
using Raftel.Server.Grpc;

namespace Raftel.Server.Storage;

[ServiceContract]
public class FolderService(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
    : CrudService<FolderDto, CreateFolderDto, FolderDto, FolderFilterDto>(queryDispatcher, commandDispatcher),
        IFolderService
{
    public override async Task<FolderDto> CreateAsync(CreateFolderDto createDto)
    {
        var command = new CreateFolderCommand(createDto.Name);

        var result = await CommandDispatcher.Dispatch(command);

        return result.Value;
    }

    public override Task<FolderDto> UpdateAsync(FolderDto dto)
    {
        return Task.FromResult<FolderDto>(null);
    }

    public override Task DeleteAsync(EntityByIdFilter filter)
    {
        return Task.CompletedTask;
    }

    public override Task<FolderDto> GetAsync(EntityByIdFilter filter)
    {
        return Task.FromResult<FolderDto>(null);
    }

    public override async Task<PagedResult<FolderDto>> GetListAsync(FolderFilterDto filter)
    {
        var folders = await QueryDispatcher.Dispatch(new GetFoldersQuery(filter.ParentFolderId));

        return new PagedResult<FolderDto>
        {
            Items = folders
        };
    }
}