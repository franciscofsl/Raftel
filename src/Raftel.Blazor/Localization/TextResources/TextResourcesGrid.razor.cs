using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Grid;
using Raftel.Blazor.Localization.TextResources.Operations.Create;
using Raftel.Blazor.Localization.TextResources.Operations.Edit;
using Raftel.Blazor.Localization.TextResources.Services;
using Raftel.Blazor.Localization.TextResources.Setups;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization.TextResources;

public partial class TextResourcesGrid
{
    private EditTextResource _editTextResource;
    private CreateTextResource _createForm;
    private SnGrid<TextResourceDto, TextResourceGridSetup, TextResourceFilterDto> _grid;

    [Inject] private ITextResourceService Service { get; set; }

    [Inject] private TextResourceGridNotifier TextResourceGridNotifier { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        TextResourceGridNotifier.Notify += RefreshAsync;
    }

    public override void Dispose()
    {
        base.Dispose();
        TextResourceGridNotifier.Notify -= RefreshAsync;
    }

    private Task RefreshAsync()
    {
        return _grid.Refresh();
    }

    private async Task<GridData<TextResourceDto>> GetAllTextResourcesAsync(TextResourceFilterDto filter)
    {
        if (Service is null)
        {
            return new GridData<TextResourceDto>();
        }
        
        var items = await Service.GetListAsync(filter);
        
        return new GridData<TextResourceDto>
        {
            Items = items.Items
        };
    }

    private async Task CreateTextResourceAsync()
    {
        await _createForm.ShowAsync();
    }

    private Task EditTextResourceAsync(TextResourceDto TextResource)
    {
        return _editTextResource.ShowAsync(TextResource.Id);
    }

    private async Task DeleteTextResourceAsync(TextResourceDto TextResource)
    {
        await Service.DeleteAsync(new EntityByIdFilter
        {
            Id = TextResource.Id
        });
        await RefreshAsync();
    }
}