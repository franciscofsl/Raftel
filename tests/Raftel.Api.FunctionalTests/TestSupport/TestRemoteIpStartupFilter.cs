using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Raftel.Api.FunctionalTests.TestSupport;

/// <summary>
/// TestServer connections have no remote IP. This filter runs before the app pipeline and sets the connection IP
/// from the <see cref="HeaderName"/> request header, so tests can simulate distinct clients.
/// </summary>
public sealed class TestRemoteIpStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var value)
                && IPAddress.TryParse(value.ToString(), out var address))
            {
                context.Connection.RemoteIpAddress = address;
            }

            await nextMiddleware();
        });
        next(app);
    };
}
