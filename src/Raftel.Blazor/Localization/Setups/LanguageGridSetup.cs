using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Grid;
using Raftel.Blazor.Grid.Columns.Types;

namespace Raftel.Blazor.Localization.Setups;

public class LanguageGridSetup : GridSetup<LanguageDto>
{
    public LanguageGridSetup()
    {
        AddColumn(_ => _.Id, new TextColumn()
        {
            DisplayName = "Languages:Id",
            Visible = false
        });
        
        AddColumn(_ => _.Name, new TextColumn()
        {
            DisplayName = "Languages:Name"
        });
        
        AddColumn(_ => _.IsoCode, new TextColumn()
        {
            DisplayName = "Languages:IsoCode"
        });
    }
}