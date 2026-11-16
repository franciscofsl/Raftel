using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raftel.Api.Server.Security;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Security;

public sealed class SecurityOptionsValidationTests
{
    [Fact]
    public async Task CredentialsWithWildcardOrigin_ShouldFailAtStartupWithAClearMessage()
    {
        await using var app = BuildApp(new Dictionary<string, string?>
        {
            ["Raftel:Security:Cors:AllowedOrigins:0"] = "*",
            ["Raftel:Security:Cors:AllowCredentials"] = "true"
        });

        var exception = await Should.ThrowAsync<OptionsValidationException>(() => app.StartAsync());

        exception.Message.ShouldContain("AllowCredentials cannot be combined with the '*' origin");
    }

    [Theory]
    [InlineData("Raftel:Security:RateLimiting:Auth:PermitLimit", "0", "PermitLimit")]
    [InlineData("Raftel:Security:RateLimiting:Write:Window", "00:00:00", "Window")]
    [InlineData("Raftel:Security:ForwardedHeaders:KnownProxies:0", "not-an-ip", "not-an-ip")]
    [InlineData("Raftel:Security:ForwardedHeaders:KnownNetworks:0", "10.0.0.0/99", "10.0.0.0/99")]
    public void InvalidSetting_ShouldFailValidation(string key, string value, string expectedInMessage)
    {
        var options = Resolve(new Dictionary<string, string?> { [key] = value });

        var exception = Should.Throw<OptionsValidationException>(() => options.Value);

        exception.Message.ShouldContain(expectedInMessage);
    }

    [Fact]
    public void Defaults_ShouldBeValidAndAllowNoOrigin()
    {
        var options = Resolve(new Dictionary<string, string?>()).Value;

        options.Cors.AllowedOrigins.ShouldBeEmpty();
        options.RateLimiting.Enabled.ShouldBeTrue();
        options.RateLimiting.Auth.PermitLimit.ShouldBe(5);
        options.RateLimiting.Auth.Window.ShouldBe(TimeSpan.FromMinutes(5));
    }

    private static IOptions<RaftelSecurityOptions> Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRaftelSecurity(configuration);
        return services.BuildServiceProvider().GetRequiredService<IOptions<RaftelSecurityOptions>>();
    }

    private static WebApplication BuildApp(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddRaftelSecurity(builder.Configuration);
        return builder.Build();
    }
}
