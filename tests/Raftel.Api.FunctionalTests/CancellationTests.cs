using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.FunctionalTests.DemoApi.TestSupport;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class CancellationTests
{
    private readonly HttpClient _client;

    public CancellationTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task PostAwaitCancellation_WhenClientAborts_ShouldNotCompleteTheOperation()
    {
        var completionsBefore = AwaitCancellationTracker.Completions;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));

        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        await Should.ThrowAsync<OperationCanceledException>(() =>
            _client.PostAsync("/api/test/cancellation", content, cts.Token));

        await Task.Delay(TimeSpan.FromSeconds(6));

        AwaitCancellationTracker.Completions.ShouldBe(completionsBefore);
    }
}
