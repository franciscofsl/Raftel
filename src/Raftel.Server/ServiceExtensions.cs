using Microsoft.AspNetCore.Builder;
using Raftel.Server.Localization;

namespace Raftel.Server;

public static class ServiceExtensions
{
    public static WebApplication ConfigureRaftelWebApp(this WebApplication app)
    {
        app.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
        app.MapGrpcService<LanguageService>();
        return app;
    }
}