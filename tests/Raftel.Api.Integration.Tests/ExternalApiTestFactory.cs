using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Integration.Tests.Api;

namespace Raftel.Api.Integration.Tests;

public sealed class ExternalApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5128"); // O el puerto que uses
    }
}