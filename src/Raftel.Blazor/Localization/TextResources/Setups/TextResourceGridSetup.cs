using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Grid;
using Raftel.Blazor.Grid.Columns.Types;

namespace Raftel.Blazor.Localization.TextResources.Setups;

public class TextResourceGridSetup : GridSetup<TextResourceDto>
{
    public TextResourceGridSetup()
    {
        AddColumn(_ => _.Id, new TextColumn()
        {
            DisplayName = "TextResources:Id",
            Visible = false
        });

        AddColumn(_ => _.Key, new TextColumn()
        {
            DisplayName = "TextResources:Key"
        });

        AddColumn(_ => _.Value, new TextColumn()
        {
            DisplayName = "TextResources:Value"
        });
    }
}