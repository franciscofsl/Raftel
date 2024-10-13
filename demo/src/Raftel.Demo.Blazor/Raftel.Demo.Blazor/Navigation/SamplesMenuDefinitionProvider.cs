using Raftel.Blazor.Menu;

namespace Raftel.Demo.Blazor.Navigation;

public class SamplesMenuDefinitionProvider : MenuDefinitionProvider
{
    public override Menu ConfigureMenu()
    {
        return new Menu()
            .CreateSubMenu(new Menu()
            {
                Icon = "fas fa-home",
                Path = "Home",
                Text = "Menu:Home"
            })
            .CreateSubMenu(new Menu()
                {
                    Text = "Menu:Administration"
                }
                .CreateSubMenu(new Menu()
                {
                    Text = "Menu:Languages",
                    Path = "Administration/Languages"
                }))
            .CreateSubMenu(BuildSamplesMenu());
    }

    private Menu BuildSamplesMenu()
    {
        return new Menu()
            {
                Text = "Menu:General",
                Icon = "fas fa-store"
            }
            .CreateSubMenu(new Menu()
            {
                Text = "Menu:Samples",
                Path = "Samples"
            });
    }
}