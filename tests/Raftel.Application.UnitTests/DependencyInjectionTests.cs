using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Application.Queries;
using Raftel.Application.UnitTests.Abstractions;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.Events;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.Events;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Validators;
using Shouldly;

namespace Raftel.Application.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddRaftelApplication_ShouldRegisterCommandHandler_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreatePirateCommand, Guid>>();

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

    [Fact]
    public void AddRaftelApplication_ShouldRegisterDomainEventHandler_FromAssembly()
    {
        var provider = BuildServiceProvider();

        var handler = provider.GetService<IDomainEventHandler<PirateCrownedKing>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<PirateCrownedKingHandler>();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterDomainEventsDispatcher()
    {
        var provider = BuildServiceProvider();

        var dispatcher = provider.GetService<IDomainEventsDispatcher>();

        dispatcher.ShouldNotBeNull();
        dispatcher.ShouldBeOfType<DomainEventsDispatcher>();
    }

    [Fact]
    public void AddRaftelApplication_WhenPermissionMiddlewareIsNotConfigured_ShouldRegisterItByDefault()
    {
        var provider = BuildServiceProviderWithTestAssembly();

        var registry = provider.GetRequiredService<MiddlewareRegistry>();
        registry.GlobalMiddlewares.ShouldContain(typeof(PermissionAuthorizationMiddleware<,>));

        using var scope = provider.CreateScope();
        var middlewares = scope.ServiceProvider.GetServices<IGlobalMiddleware<ProtectedTestCommand, Result>>();
        middlewares.ShouldContain(m => m is PermissionAuthorizationMiddleware<ProtectedTestCommand, Result>);
    }

    [Fact]
    public void AddRaftelApplication_WhenLoggingMiddlewareIsConfigured_ShouldPlacePermissionMiddlewareRightAfterIt()
    {
        var registry = BuildRegistry(cfg =>
        {
            cfg.AddGlobalMiddleware(typeof(LoggingMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
        });

        registry.GlobalMiddlewares.ShouldBe([
            typeof(LoggingMiddleware<,>),
            typeof(PermissionAuthorizationMiddleware<,>),
            typeof(ValidationMiddleware<,>)
        ]);
    }

    [Fact]
    public void AddRaftelApplication_WhenLoggingMiddlewareIsNotConfigured_ShouldPlacePermissionMiddlewareFirst()
    {
        var registry = BuildRegistry(cfg => cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>)));

        registry.GlobalMiddlewares.ShouldBe([
            typeof(PermissionAuthorizationMiddleware<,>),
            typeof(ValidationMiddleware<,>)
        ]);
    }

    [Fact]
    public void AddRaftelApplication_WhenPermissionMiddlewareIsConfiguredExplicitly_ShouldKeepItsPositionWithoutDuplicatingIt()
    {
        var registry = BuildRegistry(cfg =>
        {
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(PermissionAuthorizationMiddleware<,>));
        });

        registry.GlobalMiddlewares.ShouldBe([
            typeof(ValidationMiddleware<,>),
            typeof(PermissionAuthorizationMiddleware<,>)
        ]);
    }

    [Fact]
    public void AddRaftelApplication_WhenNoCurrentUserIsRegistered_ShouldFallBackToAnAnonymousUserWithoutPermissions()
    {
        var provider = BuildServiceProviderWithTestAssembly();

        using var scope = provider.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.HasPermission(ProtectedTestCommand.Permission).ShouldBeFalse();
    }

    [Fact]
    public void AddRaftelApplication_WhenCurrentUserIsRegisteredBefore_ShouldKeepTheHostImplementation()
    {
        var hostUser = Substitute.For<ICurrentUser>();
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddScoped(_ => hostUser);
        services.AddRaftelApplication(cfg => cfg.RegisterServicesFromAssembly(typeof(TestCommandWithResult).Assembly));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICurrentUser>().ShouldBeSameAs(hostUser);
    }

    [Fact]
    public void AddRaftelApplication_WhenCurrentUserIsRegisteredAfter_ShouldResolveTheHostImplementation()
    {
        var hostUser = Substitute.For<ICurrentUser>();
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddRaftelApplication(cfg => cfg.RegisterServicesFromAssembly(typeof(TestCommandWithResult).Assembly));
        services.AddScoped(_ => hostUser);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICurrentUser>().ShouldBeSameAs(hostUser);
    }

    [Fact]
    public async Task DispatchAsync_ProtectedCommandWithoutAnyCurrentUserRegistered_ShouldBeRejectedAsUnauthorizedWithoutRunningTheHandler()
    {
        var provider = BuildServiceProviderWithTestAssembly();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new ProtectedTestCommand());

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        provider.GetRequiredService<ISpy>().InterceptedMessages().ShouldNotContain("Protected handler");
    }

    [Fact]
    public async Task DispatchAsync_ProtectedCommandWithAuthenticatedUserMissingThePermission_ShouldBeForbiddenWithoutRunningTheHandler()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.HasPermission(ProtectedTestCommand.Permission).Returns(false);
        var provider = BuildServiceProviderWithTestAssembly(currentUser);
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new ProtectedTestCommand());

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        provider.GetRequiredService<ISpy>().InterceptedMessages().ShouldNotContain("Protected handler");
    }

    [Fact]
    public async Task DispatchAsync_ProtectedCommandWithUserHoldingThePermission_ShouldRunTheHandler()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.HasPermission(ProtectedTestCommand.Permission).Returns(true);
        var provider = BuildServiceProviderWithTestAssembly(currentUser);
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new ProtectedTestCommand());

        result.IsSuccess.ShouldBeTrue();
        provider.GetRequiredService<ISpy>().InterceptedMessages().ShouldContain("Protected handler");
    }

    [Fact]
    public async Task DispatchAsync_UnprotectedCommandWithoutAnyCurrentUserRegistered_ShouldRunTheHandler()
    {
        var provider = BuildServiceProviderWithTestAssembly();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new TestCommand());

        result.IsSuccess.ShouldBeTrue();
        provider.GetRequiredService<ISpy>().InterceptedMessages().ShouldContain("Handler");
    }

    private static MiddlewareRegistry BuildRegistry(Action<IRaftelApplicationBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(configure);
        return services.BuildServiceProvider().GetRequiredService<MiddlewareRegistry>();
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

    private static ServiceProvider BuildServiceProviderWithTestAssembly(ICurrentUser? currentUser = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        if (currentUser is not null)
        {
            services.AddScoped(_ => currentUser);
        }

        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TestCommandWithResult).Assembly);
        });

        return services.BuildServiceProvider();
    }
}