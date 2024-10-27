using Microsoft.AspNetCore.Builder;
using Raftel.Server.Localization;
using Raftel.Server.Storage;

namespace Raftel.Server;

public static class ServiceExtensions
{
    public static WebApplication ConfigureRaftelWebApp(this WebApplication app)
    {
        app.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
        app.MapGrpcService<LanguageService>();
        app.MapGrpcService<TextResourceService>();
        app.MapGrpcService<FolderService>();
        return app;
    }
}