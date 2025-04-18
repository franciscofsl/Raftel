using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Application.Tests.Common;
using Raftel.Application.Tests.Common.CreatePirate;
using Raftel.Application.Tests.Common.GetPirateById;
using Raftel.Domain.Tests.Common.Domain;
using Shouldly;

namespace Raftel.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddRaftelApplication_ShouldRegisterCommandHandler_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreatePirateCommand>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<CreatePirateCommandHandler>();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterQueryHandler_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var handler = provider.GetService<IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<GetPirateByIdQueryHandler>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<IPirateRepository>(_ => Substitute.For<IPirateRepository>());

        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
        });

        return services.BuildServiceProvider();
    }
}