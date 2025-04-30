using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Raftel.Api.FunctionalTests;

public sealed class ExternalApiTestFactory : WebApplicationFactory<FunctionalTests.DemoApi.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5128");
    }
}