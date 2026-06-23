using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Raftel.Application.Localization;
using Shouldly;

namespace Raftel.Application.UnitTests.Localization;

public class LocalizationServiceTests
{
    private readonly IMemoryCache _cache;

    public LocalizationServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public void GetString_Should_Return_Translated_Text_For_Valid_Key()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        resourceProvider
            .LoadResourceAsync("TestModule", "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "Hello", "Hello World" }
                }
            });

        var service = new LocalizationService(resourceProvider, options, _cache);

        var result = service.GetString("Hello", "en", "TestModule");

        result.ShouldBe("Hello World");
    }

    [Fact]
    public void GetString_Should_Return_Key_When_Translation_Not_Found()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false,
            UseFallbackCulture = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        resourceProvider
            .LoadResourceAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((LocalizationResource)null);

        resourceProvider
            .GetAvailableModulesAsync()
            .Returns(new List<string> { "TestModule" });

        var service = new LocalizationService(resourceProvider, options, _cache);

        var result = service.GetString("NonExistentKey", "en", "TestModule");

        result.ShouldBe("NonExistentKey");
    }

    [Fact]
    public void GetString_Should_Format_With_Arguments()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        resourceProvider
            .LoadResourceAsync("TestModule", "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "Welcome", "Welcome {0}" }
                }
            });

        var service = new LocalizationService(resourceProvider, options, _cache);

        var result = service.GetString("Welcome", "en", new object[] { "Luffy" }, "TestModule");

        result.ShouldBe("Welcome Luffy");
    }

    [Fact]
    public void GetString_Should_Fallback_To_Default_Culture()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false,
            UseFallbackCulture = true
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        
        // Spanish translation not found
        resourceProvider
            .LoadResourceAsync("TestModule", "es")
            .Returns((LocalizationResource)null);

        // English translation found
        resourceProvider
            .LoadResourceAsync("TestModule", "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "Hello", "Hello World" }
                }
            });

        resourceProvider
            .GetAvailableModulesAsync()
            .Returns(new List<string> { "TestModule" });

        var service = new LocalizationService(resourceProvider, options, _cache);

        var result = service.GetString("Hello", "es", "TestModule");

        result.ShouldBe("Hello World");
    }

    [Fact]
    public async Task GetResourcesAsync_Should_Merge_Multiple_Modules()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        
        resourceProvider
            .LoadResourceAsync("Pirates", "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "PirateNotFound", "Pirate not found" }
                }
            });

        resourceProvider
            .LoadResourceAsync("Ships", "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "ShipNotFound", "Ship not found" }
                }
            });

        var service = new LocalizationService(resourceProvider, options, _cache);

        var result = await service.GetResourcesAsync("en", new[] { "Pirates", "Ships" });

        result.Culture.ShouldBe("en");
        result.Texts.Count.ShouldBe(2);
        result.Texts["PirateNotFound"].ShouldBe("Pirate not found");
        result.Texts["ShipNotFound"].ShouldBe("Ship not found");
    }
}
