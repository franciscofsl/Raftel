using Raftel.Core.Localization;
using Raftel.Data.DataSeed;

namespace Raftel.Demo.Migrator.Seeds;

public class LocalizationSeeder(ILanguagesRepository languagesRepository) : ISeeder
{
    public async Task SeedAsync()
    {
        var languages = await languagesRepository.GetListAsync();
        foreach (var language in languages)
        {
            await languagesRepository.DeleteAsync(language);
        }

        var spanish = Language.Create("Español", "es");
        spanish.AddTranslationResource("Common:Add", "Nuevo");
        spanish.AddTranslationResource("Common:Edit", "Editar");
        spanish.AddTranslationResource("Common:Delete", "Borrar");
        spanish.AddTranslationResource("Menu:Home", "Home");
        spanish.AddTranslationResource("Menu:Administration", "Administración");
        spanish.AddTranslationResource("Menu:General", "General");
        spanish.AddTranslationResource("Menu:Languages", "Lenguajes");
        spanish.AddTranslationResource("Languages:Name", "Nombre");
        spanish.AddTranslationResource("Languages:IsoCode", "Codigo ISO");
        spanish.AddTranslationResource("Languages:NewLanguage", "Nuevo lenguaje");
        spanish.AddTranslationResource("Languages:EditLanguage", "Editar lenguaje");
        spanish.AddTranslationResource("TextResources:Key", "Clave");
        spanish.AddTranslationResource("TextResources:Value", "Valor");
        await languagesRepository.InsertAsync(spanish);
    }
}