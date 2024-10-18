using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Forms;
using Raftel.Blazor.Forms.Configurators;
using Raftel.Blazor.Forms.Fields.Types;
using Raftel.Blazor.Localization.TextResources.Services;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization.TextResources.Operations.Create;

public partial class CreateTextResource
{
    private SnTypedModalForm<CreateTextResourceDto> _form;
    private CreateTextResourceDto _item = new();

    [Inject] private TextResourceGridNotifier TextResourceGridNotifier { get; set; }

    [Inject] private ITextResourceService Service { get; set; }

    [Parameter] public LanguageDto Language { get; set; }

    public Task ShowAsync()
    {
        return _form.ShowAsync();
    }

    private async Task SaveAsync(CreateTextResourceDto item)
    {
        item.LanguageId = Language.Id;
        await Service.CreateAsync(item);
        await TextResourceGridNotifier.Update();
    }

    public FormConfiguration<CreateTextResourceDto> GetConfiguration()
    {
        return FormConfigurator<CreateTextResourceDto>
            .Create()
            .AddGroup(_ => _
                .Title("TextResource.Create")
                .Fields(c => c
                    .Add(t => t.Key, new TextField()
                    {
                        DisplayName = "TextResources.Key"
                    })
                    .Add(t => t.Value, new TextField()
                    {
                        DisplayName = "TextResources.Value"
                    })))
            .Configure();
    }
}