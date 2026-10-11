using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Raftel.Api.Server.Health;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static Task WriteStatusOnly(HttpContext httpContext, HealthReport report)
    {
        var payload = new
        {
            status = report.Status,
            totalDurationMs = report.TotalDuration.TotalMilliseconds
        };

        return WriteJson(httpContext, payload);
    }

    public static Task WriteDetailed(HttpContext httpContext, HealthReport report)
    {
        var payload = new
        {
            status = report.Status,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status,
                    durationMs = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description
                })
        };

        return WriteJson(httpContext, payload);
    }

    private static Task WriteJson(HttpContext httpContext, object payload)
    {
        httpContext.Response.ContentType = "application/json";
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
