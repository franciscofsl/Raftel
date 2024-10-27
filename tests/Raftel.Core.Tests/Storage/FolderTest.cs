using Raftel.Core.Storage;

namespace Raftel.Core.Tests.Storage;

public class FolderTests
{
    [Fact]
    public void CreateFolder_ShouldCreateSubFolder_WhenNameIsUnique()
    {
        var parentFolder = Folder.Create("Parent");
        var result = parentFolder.CreateFolder("SubFolder");

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("SubFolder");
        parentFolder.SubFolders.Should().ContainSingle().Which.Should().BeEquivalentTo(result.Value);
    }

    [Fact]
    public void CreateFolder_ShouldFail_WhenNameIsDuplicated()
    {
        var parentFolder = Folder.Create("Parent");
        parentFolder.CreateFolder("SubFolder");

        var result = parentFolder.CreateFolder("SubFolder");

        result.Success.Should().BeFalse();
        result.Error.Code.Should().Be(StorageErrors.DuplicatedFolder);
        parentFolder.SubFolders.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveSubFolder_ShouldRemoveFolder_WhenFolderExists()
    {
        var parentFolder = Folder.Create("Parent");
        var subFolder = parentFolder.CreateFolder("SubFolder").Value;

        parentFolder.RemoveSubFolder(subFolder);

        parentFolder.SubFolders.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSubFolder_ShouldNotRemoveFolder_WhenFolderDoesNotExist()
    {
        var parentFolder = Folder.Create("Parent");
        var nonExistentFolder = Folder.Create("NonExistentFolder");

        parentFolder.RemoveSubFolder(nonExistentFolder);

        parentFolder.SubFolders.Should().BeEmpty();
    }

    [Fact]
    public void AddDocument_ShouldAddDocument_WhenValidParametersAreProvided()
    {
        var folder = Folder.Create("Documents");
        var content = "Test content for blob storage."u8.ToArray();
        var result = folder.AddDocument("Document1", content);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Document1");
        folder.Documents.Should().ContainSingle().Which.Should().BeEquivalentTo(result.Value);
    }

    [Fact]
    public void RemoveDocument_ShouldRemoveDocument_WhenDocumentExists()
    {
        var folder = Folder.Create("Documents");

        var content = "Test content for blob storage."u8.ToArray();
        var document = folder.AddDocument("Document1", content).Value;

        folder.RemoveDocument(document);

        folder.Documents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDocument_ShouldNotRemoveDocument_WhenDocumentDoesNotExist()
    {
        var folder = Folder.Create("Documents");
        var folder2 = Folder.Create("Documents 2");
        var content = "Test content for blob storage."u8.ToArray();
        var nonExistentDocument = folder2.AddDocument("NonExistentDocument", content);

        folder.RemoveDocument(nonExistentDocument.Value);

        folder.Documents.Should().BeEmpty();
    }
}