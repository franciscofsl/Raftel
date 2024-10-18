using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc.Client;
using Raftel.Blazor.Localization.Services;
using Refit;
using Raftel.Blazor.Services;
using Raftel.Blazor.Services.LocalStorage;
using Raftel.Blazor.Services.LocalStorage.JsonConverters;
using Raftel.Blazor.Services.LocalStorage.Serialization;
using Raftel.Blazor.Services.LocalStorage.StorageOptions;
using Raftel.Blazor.Shared.Localization;
using Raftel.Blazor.Toast;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

namespace Raftel.Blazor;

public static class ServiceExtensions
{
    public static void AddRaftelBlazor(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IStringLocalizerFactory, InMemoryStringLocalizerFactory>();
        builder.Services.AddSingleton<IStringLocalizer, InMemoryLocalizer>(serviceProvider =>
        {
            var translationsService = serviceProvider.GetService<ITextResourceService>();

            var items = translationsService
                .GetListAsync(new TextResourceFilterDto
                {
                    LanguageId = Guid.Parse("0cebb178-3908-23b5-4cce-3a15973a37ae")
                })
                .GetAwaiter()
                .GetResult();

            var dictionary = items.Items.ToDictionary(_ => _.Key, _ => _.Value);
            return new InMemoryLocalizer(dictionary);
        });
        
        var license = builder.Configuration["SyncfusionLicense"];
        SyncfusionLicenseProvider.RegisterLicense(license);
        builder.Services.AddSyncfusionBlazor();
        builder.Services.AddLocalStorage();

        builder.Services.AddSingleton<ToastNotifier>();
        builder.Services.AddSingleton<ToastService>();

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(_ =>
            {
                var name = _.GetName().Name;
                return name != null && (name.Contains("Blazor"));
            })
            .ToList();
        ConfigureEventNotifiers(builder, assemblies);
        ConfigureGrpcChannel(builder.Services);
    }

    private static void ConfigureGrpcChannel(IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var config = _.GetRequiredService<IConfiguration>();
            var backendUrl = config["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                var navigationManager = _.GetRequiredService<NavigationManager>();
                backendUrl = navigationManager.BaseUri;
            }

            var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

            return GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });
        });
    }

    private static void ConfigureEventNotifiers(WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.Services
            .Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(EventNotifier)))
                .AsSelf()
                .WithSingletonLifetime());
    }

    public static void AddRestClient<TService>(this WebApplicationBuilder builder, string uri = null)
        where TService : class, IRestService
    {
        builder.Services.AddRefitClient<TService>().ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri(uri ?? builder.Configuration["BackendUrl"]);
        });
    }

    public static void AddGrpcService<TService>(this IServiceCollection services) where TService : class
    {
        services.AddTransient(s =>
        {
            var grpcChannel = s.GetRequiredService<GrpcChannel>();
            return grpcChannel.CreateGrpcService<TService>();
        });
    }

    private static void AddLocalStorage(this IServiceCollection services)
    {
        services.AddScoped<IJsonSerializer, SystemTextJsonSerializer>();
        services.AddScoped<IStorageProvider, BrowserStorageProvider>();
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddScoped<ISyncLocalStorageService, LocalStorageService>();

        if (services.All(_ => _.ServiceType != typeof(IConfigureOptions<LocalStorageOptions>)))
        {
            services.Configure<LocalStorageOptions>(_ =>
            {
                _.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                _.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                _.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                _.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                _.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                _.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                _.JsonSerializerOptions.WriteIndented = false;
                _.JsonSerializerOptions.Converters.Add(new TimespanJsonConverter());
            });
        }
    }
}