using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Application.Localization;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class LocalizationEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public LocalizationEndpointsTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task GetResources_Should_Return_English_Translations_By_Default()
    {
        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("en");
        response.Texts.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetResources_Should_Return_Spanish_Translations_When_Specified()
    {
        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources?cultureName=es");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("es");
        response.Texts.ShouldNotBeEmpty();
        response.Texts.ShouldContainKey("PirateNotFound");
        response.Texts["PirateNotFound"].ShouldBe("Pirata no encontrado");
    }

    [Fact]
    public async Task GetResources_Should_Filter_By_Module()
    {
        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources?cultureName=en&modules=Pirates");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("en");
        response.Texts.ShouldContainKey("PirateNotFound");
        response.Texts["PirateNotFound"].ShouldBe("Pirate not found");
    }

    [Fact]
    public async Task GetResourcesByCulture_Should_Return_French_Translations()
    {
        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources/fr");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("fr");
        response.Texts.ShouldNotBeEmpty();
        response.Texts.ShouldContainKey("PirateNotFound");
        response.Texts["PirateNotFound"].ShouldBe("Pirate non trouvé");
    }

    [Fact]
    public async Task GetResources_Should_Use_Accept_Language_Header()
    {
        _client.DefaultRequestHeaders.Add("Accept-Language", "es");

        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("es");
    }

    [Fact]
    public async Task GetResources_Should_Merge_Multiple_Modules()
    {
        var response = await _client.GetFromJsonAsync<LocalizationResource>("/api/localization/resources?cultureName=en&modules=Pirates,Ships");

        response.ShouldNotBeNull();
        response.Culture.ShouldBe("en");
        response.Texts.ShouldContainKey("PirateNotFound");
        response.Texts.ShouldContainKey("ShipNotFound");
        response.Texts["PirateNotFound"].ShouldBe("Pirate not found");
        response.Texts["ShipNotFound"].ShouldBe("Ship not found");
    }
}
