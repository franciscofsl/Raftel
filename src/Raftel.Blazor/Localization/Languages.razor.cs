using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Grid;
using Raftel.Blazor.Localization.Operations.Create;
using Raftel.Blazor.Localization.Operations.Edit;
using Raftel.Blazor.Localization.Services;
using Raftel.Blazor.Localization.Setups;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization;

public partial class Languages
{
    private EditLanguage _editLanguage;
    private CreateLanguage _createForm;
    private SnGrid<LanguageDto, LanguageGridSetup, LanguageFilterDto> _grid;

    [Inject] private ILanguageService Service { get; set; }

    [Inject] private LanguageGridNotifier LanguageGridNotifier { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LanguageGridNotifier.Notify += RefreshAsync;
    }

    public override void Dispose()
    {
        base.Dispose();
        LanguageGridNotifier.Notify -= RefreshAsync;
    }

    private Task RefreshAsync()
    {
        return _grid.Refresh();
    }

    private async Task<GridData<LanguageDto>> GetAllLanguagesAsync(LanguageFilterDto filter)
    {
        if (Service is null)
        {
            return new GridData<LanguageDto>();
        }
        
        var items = await Service.GetListAsync(filter);
        
        return new GridData<LanguageDto>
        {
            Items = items.Items
        };
    }

    private async Task CreateLanguageAsync()
    {
        await _createForm.ShowAsync();
    }

    private Task EditLanguageAsync(LanguageDto language)
    {
        return _editLanguage.ShowAsync(language.Id);
    }

    private async Task DeleteLanguageAsync(LanguageDto language)
    {
        await Service.DeleteAsync(new EntityByIdFilter
        {
            Id = language.Id
        });
        await RefreshAsync();
    }
}