using NSubstitute;
using Raftel.Application.BlobStorage.Commands.CreateFolder;
using Raftel.Core.Storage;

namespace Raftel.Application.Tests.BlobStorage;

public class CreateFolderCommandHandlerTest
{
    [Fact]
    public async Task Command_ShouldCreateFolder()
    {
        var folderStore = Substitute.For<IFolderStore>();
        var handler = new CreateFolderCommandHandler(folderStore);

        var result = await handler.Handle(new CreateFolderCommand("Test"));
        result.Success.Should().BeTrue();
        folderStore.Received(1).AddAsync(Arg.Any<Folder>());
    }
}