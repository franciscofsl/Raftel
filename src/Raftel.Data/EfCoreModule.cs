using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Core.Auditing;
using Raftel.Core.Contracts;
using Raftel.Core.Localization;
using Raftel.Core.Storage;
using Raftel.Data.DbContexts.Auditing;
using Raftel.Data.DbContexts.BlobStorage;
using Raftel.Data.Outbox;
using Raftel.Data.Repositories;
using Raftel.Data.Repositories.Localization;
using Raftel.Shared.Modules;

namespace Raftel.Data;

public abstract class EfCoreModule : RaftelModule
{
    protected EfCoreModule()
    {
        AssembliesToLoadRepositories = Enumerable.Empty<Assembly>().ToList();
    }

    protected virtual IReadOnlyList<Assembly> AssembliesToLoadRepositories { get; }

    public override void ConfigureCustomServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddTransient<ILanguagesRepository, LanguagesRepository>();
        services.AddSingleton<ConvertDomainEventsToOutboxMessagesInterceptor>();
        services.AddTransient<AuditChangesStore>();
        services.AddTransient<IEntityChangesReader, EntityChangesReader>();
        services.AddTransient<IFolderStore, FolderStore>();

        services.Scan(scan => scan
            .FromAssemblies(AssembliesToLoadRepositories)
            .AddClasses(classes => classes.AssignableTo(typeof(IRepository<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
    }
}