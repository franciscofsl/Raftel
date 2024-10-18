using Raftel.Core.Localization;

namespace Raftel.Core.Tests.Localization;

public class LanguageTest
{
    [Fact]
    public void Language_Constructor_Test()
    {
        var language = Language.Create("Spanish", "es");

        language.Name.Should().Be("Spanish");
        language.IsoCode.Should().Be("es");
    }

    [Fact]
    public void Should_Add_Resource_In_Language()
    {
        var language = Language.Create("Spanish", "es");

        var result = language.AddTranslationResource("Common:Name", "Nombre");

        result.Value.Key.Should().Be("Common:Name");
        result.Value.Value.Should().Be("Nombre");
        language.Resources.Should().ContainSingle();
    }

    [Fact]
    public void Should_Not_Add_Duplicated_Resource_By_Key()
    {
        var language = Language.Create("Spanish", "es");

        var resource = language.AddTranslationResource("Common:Name", "Nombre");

        var result = language.AddTranslationResource(resource.Value.Key, "Nombre");
        result.Error.Code.Should().Be(LocalizationErrors.DuplicatedResource);
    }
}