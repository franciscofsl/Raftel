using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Raftel.Application.Localization;
using Shouldly;

namespace Raftel.Application.UnitTests.Localization;

public class RaftelStringLocalizerTests
{
    [Fact]
    public void Indexer_Should_Return_Localized_String()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        resourceProvider
            .LoadResourceAsync(Arg.Any<string>(), "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "Hello", "Hello World" }
                }
            });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var localizationService = new LocalizationService(resourceProvider, options, cache);
        var localizer = new RaftelStringLocalizer<TestModule>(localizationService);

        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = localizer["Hello"];

        result.Value.ShouldBe("Hello World");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_With_Arguments_Should_Format_String()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            EnableCaching = false
        });

        var resourceProvider = Substitute.For<IResourceProvider>();
        resourceProvider
            .LoadResourceAsync(Arg.Any<string>(), "en")
            .Returns(new LocalizationResource
            {
                Culture = "en",
                Texts = new Dictionary<string, string>
                {
                    { "Welcome", "Welcome {0}" }
                }
            });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var localizationService = new LocalizationService(resourceProvider, options, cache);
        var localizer = new RaftelStringLocalizer<TestModule>(localizationService);

        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = localizer["Welcome", "Luffy"];

        result.Value.ShouldBe("Welcome Luffy");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_Should_Return_Key_When_Not_Found()
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

        var cache = new MemoryCache(new MemoryCacheOptions());
        var localizationService = new LocalizationService(resourceProvider, options, cache);
        var localizer = new RaftelStringLocalizer<TestModule>(localizationService);

        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = localizer["NonExistentKey"];

        result.Value.ShouldBe("NonExistentKey");
        result.ResourceNotFound.ShouldBeTrue();
    }

    private class TestModule { }
}
