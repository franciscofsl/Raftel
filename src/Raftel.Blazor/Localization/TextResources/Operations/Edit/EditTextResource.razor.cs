using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Forms;
using Raftel.Blazor.Forms.Configurators;
using Raftel.Blazor.Forms.Fields.Types;
using Raftel.Blazor.Localization.TextResources.Services;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;

namespace Raftel.Blazor.Localization.TextResources.Operations.Edit;

public partial class EditTextResource
{
    private SnAdvancedModalForm<TextResourceDto> _form;

    [Inject] private TextResourceGridNotifier TextResourceGridNotifier { get; set; }

    [Inject] private ITextResourceService Service { get; set; }

    public Task ShowAsync(Guid id)
    {
        return _form.ShowAsync(id);
    }

    private async Task<TextResourceDto> GetTextResourceAsync(Guid id)
    {
        return await Service.GetAsync(new EntityByIdFilter
        {
            Id = id
        });
    }

    private async Task SaveAsync(TextResourceDto item)
    {
        await Service.UpdateAsync(item);
        await TextResourceGridNotifier.Update();
        await _form.CloseAsync();
    }

    private FormConfiguration<TextResourceDto> GetConfiguration()
    {
        return FormConfigurator<TextResourceDto>
            .Create()
            .AddGroup(_ => _
                .Title("TextResources.Create")
                .Fields(c => c
                    .Add(t => t.Key, new TextField()
                    {
                        DisplayName = "TextResources.IsoCode"
                    })
                    .Add(t => t.Value, new TextField()
                    {
                        DisplayName = "TextResources.Name"
                    })))
            .Configure();
    }
}