namespace Raftel.Demo.Blazor.Pages.Storage;

public partial class SnStorageExplorer
{
    private List<FolderDto> _folders = Enumerable
        .Range(0, 5)
        .Select(_ => new FolderDto()
        {
            Name = $"Folder {_}",
        })
        .ToList();

    private List<FileDto> _files = Enumerable
        .Range(0, 30)
        .Select(_ => new FileDto()
        {
            Name = $"File {_}",
        })
        .ToList();

 
}

public class FolderDto
{
    public string Name { get; set; }
    public bool IsSelected { get; set; }
}

public class FileDto
{
    public string Name { get; set; }
}