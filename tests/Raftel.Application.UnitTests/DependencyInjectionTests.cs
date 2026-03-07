using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Application.Queries;
using Raftel.Application.UnitTests.Abstractions;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Validators;
using Shouldly;

namespace Raftel.Application.UnitTests;

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
    public void AddRaftelApplication_ShouldRegisterCommandHandlerWithResult_FromAssembly()
    {
        var provider = BuildServiceProviderWithTestAssembly();

        var handler = provider.GetService<ICommandHandler<TestCommandWithResult, string>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestCommandWithResultHandler>();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterQueryHandler_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var handler = provider.GetService<IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<GetPirateByIdQueryHandler>();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterValidator_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var validator = provider.GetService<Validator<CreatePirateCommand>>();

        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<CreatePirateCommandValidator>();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterMiddlewares_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var validator = provider.GetService<Validator<CreatePirateCommand>>();

        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<CreatePirateCommandValidator>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<IPirateRepository>(_ => Substitute.For<IPirateRepository>());

        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>)); 
        });

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildServiceProviderWithTestAssembly()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();

        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TestCommandWithResult).Assembly);
        });

        return services.BuildServiceProvider();
    }
}