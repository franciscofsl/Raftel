using Microsoft.Extensions.Options;
using Raftel.Application.Localization;
using Shouldly;

namespace Raftel.Application.UnitTests.Localization;

public class JsonResourceProviderTests
{
    [Fact]
    public async Task LoadResourceAsync_Should_Load_Valid_Json_File()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LocalizationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var localizationPath = Path.Combine(tempDir, "Localization", "Pirates");
            Directory.CreateDirectory(localizationPath);

            var jsonContent = @"{
  ""culture"": ""en"",
  ""texts"": {
    ""PirateNotFound"": ""Pirate not found""
  }
}";
            await File.WriteAllTextAsync(Path.Combine(localizationPath, "en.json"), jsonContent);

            var options = Options.Create(new LocalizationOptions { ResourcesPath = "Localization" });
            var provider = new JsonResourceProvider(options, new[] { tempDir });

            var resource = await provider.LoadResourceAsync("Pirates", "en");

            resource.ShouldNotBeNull();
            resource.Culture.ShouldBe("en");
            resource.Texts["PirateNotFound"].ShouldBe("Pirate not found");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadResourceAsync_Should_Return_Null_When_File_Not_Found()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LocalizationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var options = Options.Create(new LocalizationOptions { ResourcesPath = "Localization" });
            var provider = new JsonResourceProvider(options, new[] { tempDir });

            var resource = await provider.LoadResourceAsync("NonExistent", "en");

            resource.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetAvailableModulesAsync_Should_Return_All_Module_Directories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LocalizationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var localizationPath = Path.Combine(tempDir, "Localization");
            Directory.CreateDirectory(Path.Combine(localizationPath, "Pirates"));
            Directory.CreateDirectory(Path.Combine(localizationPath, "Ships"));

            var options = Options.Create(new LocalizationOptions { ResourcesPath = "Localization" });
            var provider = new JsonResourceProvider(options, new[] { tempDir });

            var modules = await provider.GetAvailableModulesAsync();

            modules.ShouldContain("Pirates");
            modules.ShouldContain("Ships");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
