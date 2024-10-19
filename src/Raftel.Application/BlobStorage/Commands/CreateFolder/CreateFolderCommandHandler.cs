using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Storage;
using Raftel.Shared.Results;

namespace Raftel.Application.BlobStorage.Commands.CreateFolder;

public class CreateFolderCommandHandler(IFolderStore folderStore) : ICommandHandler<CreateFolderCommand, Result>
{
    public async Task<Result> Handle(CreateFolderCommand command, CancellationToken token = default)
    {
        var folder = Folder.Create(command.Name);

        await folderStore.AddAsync(folder);

        return Result.Ok();
    }
}