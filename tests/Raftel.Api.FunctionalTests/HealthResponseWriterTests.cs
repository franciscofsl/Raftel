using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Api.Server.Health;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class HealthResponseWriterTests
{
    private static HealthReport CreateReport(HealthStatus status, Exception exception = null)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(
                status,
                description: exception is null ? "ok" : "Database connection failed.",
                duration: TimeSpan.FromMilliseconds(5),
                exception: exception,
                data: null)
        };

        return new HealthReport(entries, TimeSpan.FromMilliseconds(5));
    }

    private static async Task<JsonDocument> InvokeAsync(Func<HttpContext, HealthReport, Task> writer, HealthReport report)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await writer(httpContext, report);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json);
    }

    [Fact]
    public async Task WriteStatusOnly_ShouldContainStatusAndDuration_ButNoEntries()
    {
        var report = CreateReport(HealthStatus.Healthy);

        using var document = await InvokeAsync(HealthResponseWriter.WriteStatusOnly, report);

        document.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
        document.RootElement.TryGetProperty("totalDurationMs", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("entries", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task WriteDetailed_ShouldContainEntries_WithStatusAndDuration()
    {
        var report = CreateReport(HealthStatus.Degraded);

        using var document = await InvokeAsync(HealthResponseWriter.WriteDetailed, report);

        document.RootElement.GetProperty("status").GetString().ShouldBe("Degraded");
        var entry = document.RootElement.GetProperty("entries").GetProperty("database");
        entry.GetProperty("status").GetString().ShouldBe("Degraded");
        entry.TryGetProperty("durationMs", out _).ShouldBeTrue();
        entry.GetProperty("description").GetString().ShouldBe("ok");
    }

    [Fact]
    public async Task WriteDetailed_ShouldNotContainExceptionText_WhenEntryCarriesException()
    {
        const string secret = "Password=super-secret;";
        var report = CreateReport(HealthStatus.Unhealthy, new InvalidOperationException(secret));

        using var document = await InvokeAsync(HealthResponseWriter.WriteDetailed, report);

        var raw = document.RootElement.GetRawText();
        raw.ShouldNotContain(secret);
        raw.ShouldNotContain("InvalidOperationException");
    }
}
