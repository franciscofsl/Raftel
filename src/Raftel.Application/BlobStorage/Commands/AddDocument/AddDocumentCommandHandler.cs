using Raftel.Application.BlobStorage.Contracts;
using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Storage;
using Raftel.Shared.Results;

namespace Raftel.Application.BlobStorage.Commands.AddDocument;

public class AddDocumentCommandHandler(IFolderStore folderStore, IAzureBlobStorageClient azureBlobStorageClient)
    : ICommandHandler<AddDocumentCommand, Result>
{
    public async Task<Result> Handle(AddDocumentCommand command, CancellationToken token = default)
    {
        var folder = await folderStore.GetAsync(command.FolderId);
        var document = folder.AddDocument(command.Name, command.Content);

        await azureBlobStorageClient.UploadAsync(document.Value.CalculatePointer(), command.Content);

        return Result.Ok();
    }
}