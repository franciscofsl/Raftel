using Microsoft.Extensions.Configuration;
using NSubstitute;
using Raftel.Shared.Modules;

namespace Raftel.Core.Tests.Modules;

public class RaftelApplicationTest
{
    [Fact]
    public void AddRaftelApplication_Should_Return_Application()
    {
        var serviceCollection = new ServiceCollection();

        var application = serviceCollection.AddRaftelApplication<FakeApplication>(Substitute.For<IConfiguration>());

        application.Should().NotBeNull();
    }

    [ModulesToInclude(typeof(FakeModule))]
    private class FakeApplication : RaftelApplication
    {
    }

    private class FakeModule : RaftelModule
    {
    }
}