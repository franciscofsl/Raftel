using Microsoft.Extensions.Configuration;
using NSubstitute;
using Raftel.Shared.Modules;

namespace Raftel.Core.Tests.Modules;

public class RaftelModuleTest
{
    [Fact]
    public void ConfigureCustomServices_Should_Not_Throw_Exception()
    {
        var module = new TestRaftelModule();

        FluentActions
            .Invoking(() => module.ConfigureCustomServices(new ServiceCollection(), Substitute.For<IConfiguration>()))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void ConfigureServices_Should_Not_Throw_Exception()
    {
        var module = new TestRaftelModule();

        FluentActions
            .Invoking(() => module.ConfigureServices(new ServiceCollection(), Substitute.For<IConfiguration>()))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void GetModules_With_Modules_To_Include_Attribute_Returns_Modules()
    {
        var module = new TestRaftelModuleWithAttribute();

        var modules = module.GetModules();

        modules.Should().NotBeNull();
        modules.Should().NotBeEmpty();
    }

    [Fact]
    public void ConfigureServices_Should_Not_Throw_Exception_With_Other_Module()
    {
        var module = new TestRaftelModuleWithAttribute();

        FluentActions
            .Invoking(() => module.ConfigureServices(new ServiceCollection(), Substitute.For<IConfiguration>()))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void GetModules_Without_Modules_To_Include_Attribute_Returns_Empty_List()
    {
        var module = new TestRaftelModule();

        var modules = module.GetModules();

        modules.Should().BeEmpty();
    }

    private class TestRaftelModule : RaftelModule
    {
    }

    [ModulesToInclude(typeof(TestRaftelModule))]
    private class TestRaftelModuleWithAttribute : RaftelModule
    {
    }
}