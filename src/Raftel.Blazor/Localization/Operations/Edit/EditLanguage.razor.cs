using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Forms;
using Raftel.Blazor.Forms.Configurators;
using Raftel.Blazor.Forms.Fields.Types;
using Raftel.Blazor.Localization.Services;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization.Operations.Edit;

public partial class EditLanguage
{
    private SnAdvancedModalForm<LanguageDto> _form;

    [Inject] private LanguageGridNotifier LanguageGridNotifier { get; set; }

    [Inject] private ILanguageService Service { get; set; }

    public Task ShowAsync(Guid id)
    {
        return _form.ShowAsync(id);
    }

    private async Task<LanguageDto> GetLanguageAsync(Guid id)
    {
        return await Service.GetAsync(new EntityByIdFilter
        {
            Id = id
        });
    }

    private async Task SaveAsync(LanguageDto item)
    {
        await Service.UpdateAsync(item);
        await LanguageGridNotifier.Update();
        await _form.CloseAsync();
    }

    private FormConfiguration<LanguageDto> GetConfiguration()
    {
        return FormConfigurator<LanguageDto>
            .Create()
            .AddGroup(_ => _
                .Title("Languages:EditLanguage")
                .Fields(c => c
                    .Add(t => t.IsoCode, new TextField()
                    {
                        DisplayName = "Languages:IsoCode"
                    })
                    .Add(t => t.Name, new TextField()
                    {
                        DisplayName = "Languages:Name"
                    })))
            .Configure();
    }
}