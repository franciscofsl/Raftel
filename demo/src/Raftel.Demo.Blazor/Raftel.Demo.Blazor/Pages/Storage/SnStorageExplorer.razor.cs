using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Raftel.Application.Contracts.Storage;
using Raftel.Blazor.Shared.Storage;
using Raftel.Shared.Extensions;

namespace Raftel.Demo.Blazor.Pages.Storage;

public partial class SnStorageExplorer
{
    private List<FolderDto> _folders = new();
    private List<FileDto> _files = new();

    [Inject] private IFolderService FolderService { get; set; }

    protected override Task OnInitializedAsync()
    {
        LoadFoldersAsync().Forget();
        return base.OnInitializedAsync();
    }

    private async Task LoadFoldersAsync()
    {
        var folders = await FolderService.GetListAsync(new FolderFilterDto());
        _folders = folders.Items;
    }

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

    private Task NewFolder()
    {
        return FolderService.CreateAsync(new CreateFolderDto()
        {
            Name = $"NEW FOLDER AT {DateTime.Now}"
        });
    }
}

public class FileDto
{
    public string Name { get; set; }
    public DateTime UploadedAt { get; set; }
}