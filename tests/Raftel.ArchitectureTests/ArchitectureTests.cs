using System.Reflection;
using NetArchTest.Rules;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application;
using Raftel.Domain.BaseTypes;
using Shouldly;

namespace Raftel.ArchitectureTests;

public class ArchitectureTests
{
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private const string DomainNamespace = "Raftel.Domain";
    private const string ApplicationNamespace = "Raftel.Application";
    private const string InfrastructureNamespace = "Raftel.Infrastructure";

    private static readonly Assembly DomainAssembly = typeof(AggregateRoot<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(RaftelApplicationBuilder).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Raftel.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(AutoEndpointGroupExtensions).Assembly;

    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("Domain must not depend on other layers", result));
    }

    [Fact]
    public void Domain_Should_Not_DependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(EntityFrameworkCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("EF Core is prohibited in Domain", result));
    }

    [Fact]
    public void Application_Should_Not_DependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace)
            .And()
            .NotHaveDependencyOn(ApiAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("Application must only depend on Domain", result));
    }

    [Fact]
    public void Application_Should_Not_DependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(EntityFrameworkCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("EF Core is prohibited in Application", result));
    }

    [Fact]
    public void Application_Should_Not_DependOn_AspNetCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(AspNetCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("ASP.NET Core is prohibited in Application", result));
    }

    [Fact]
    public void Api_Should_Not_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("Api.Server must not depend on Infrastructure", result));
    }

    [Fact]
    public void Api_Should_Not_DependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn(EntityFrameworkCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage("EF Core is prohibited in Api.Server", result));
    }

    private static string FailureMessage(string rule, TestResult result)
    {
        var offenders = result.FailingTypeNames is null
            ? "-"
            : string.Join(", ", result.FailingTypeNames);

        return $"{rule}. Bad types: {offenders}";
    }
}
