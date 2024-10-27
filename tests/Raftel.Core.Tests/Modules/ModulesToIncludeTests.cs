using Microsoft.Extensions.Configuration;
using Raftel.Shared.Modules;

namespace Raftel.Core.Tests.Modules;

public class ModulesToIncludeTests
{
    [Fact]
    public void Constructor_With_Valid_Module_Types_Creates_Modules_List()
    {
        var moduleType = typeof(TestRaftelModule);

        var attribute = new ModulesToIncludeAttribute(moduleType);

        attribute.Modules.Should().NotBeNull();
        attribute.Modules.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_With_No_Module_Types_Throws_InvalidOperationException()
    {
        FluentActions.Invoking(() => new ModulesToIncludeAttribute())
            .Should()
            .Throw<InvalidOperationException>();
    }

    [ModulesToInclude(typeof(TestRaftelModule))]
    private class TestRaftelModule : RaftelModule
    {
    }
}