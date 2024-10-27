using NSubstitute;
using Raftel.Application.BlobStorage.Commands.AddDocument;
using Raftel.Application.BlobStorage.Contracts;
using Raftel.Core.Storage;

namespace Raftel.Application.Tests.BlobStorage;

public class AddDocumentCommandHandlerTest
{
    [Fact]
    public async Task Command_ShouldAddFolder()
    {
        var folderStore = Substitute.For<IFolderStore>();
        var blobStorageClient = Substitute.For<IAzureBlobStorageClient>();
        var handler = new AddDocumentCommandHandler(folderStore, blobStorageClient);

        folderStore.GetAsync(Arg.Any<Guid?>()).Returns(Folder.Create("New Folder"));
        
        var content = "Test content for blob storage."u8.ToArray();
        var command = new AddDocumentCommand(null, "Test.txt", content);
        var result = await handler.Handle(command);
        result.Success.Should().BeTrue();
        blobStorageClient.Received(1).UploadAsync(Arg.Any<string>(), Arg.Any<byte[]>());
    }
}