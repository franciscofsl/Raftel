using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Forms;
using Raftel.Blazor.Forms.Configurators;
using Raftel.Blazor.Forms.Fields.Types;
using Raftel.Blazor.Localization.Services;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization.Operations.Create;

public partial class CreateLanguage
{
    private SnTypedModalForm<CreateLanguageDto> _form;
    private CreateLanguageDto _item = new();

    [Inject] private LanguageGridNotifier LanguageGridNotifier { get; set; }

    [Inject] private ILanguageService Service { get; set; }

    public Task ShowAsync()
    {
        return _form.ShowAsync();
    }

    private async Task SaveAsync(CreateLanguageDto item)
    {
        await Service.CreateAsync(item);
        await LanguageGridNotifier.Update();
    }

    public FormConfiguration<CreateLanguageDto> GetConfiguration()
    {
        return FormConfigurator<CreateLanguageDto>
            .Create()
            .AddGroup(_ => _
                .Title("Languages:NewLanguage")
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