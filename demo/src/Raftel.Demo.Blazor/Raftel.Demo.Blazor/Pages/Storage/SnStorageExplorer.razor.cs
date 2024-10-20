using Microsoft.AspNetCore.Components.Web;

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
        .Range(0, 0)
        .Select(_ => new FileDto()
        {
            Name = $"File {_}",
        })
        .ToList();


    private void OnDragOver(DragEventArgs e)
    {
        e.DataTransfer.DropEffect = "copy";
    }

    private void OnDragLeave(DragEventArgs e)
    {
        // Opcional: puedes cambiar el estilo o mostrar algo cuando el archivo se deja de arrastrar
    }

    private async Task OnDrop(DragEventArgs e)
    {
        if (e.DataTransfer != null && e.DataTransfer.Files.Length > 0)
        {
            var files = e.DataTransfer.Files;

            foreach (var file in files)
            {
                await using var fileInfo = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = Path.GetFileName(fileInfo.Name);
                using var memoryStream = new MemoryStream();
                await fileInfo.CopyToAsync(memoryStream);
                var content = memoryStream.ToArray();

                _files.Add(new FileDto()
                {
                    Name = fileName,
                    UploadedAt = DateTime.Now
                });
            }
        }
    }
}

public class FolderDto
{
    public string Name { get; set; }
    public bool IsSelected { get; set; }
}

public class FileDto
{
    public string Name { get; set; }
    public DateTime UploadedAt { get; set; }
}