using System.Text.Json.Serialization;

namespace Raftel.Api.Integration.Tests.ApiDefinition;

public class SwaggerPathMethod
{
    [JsonPropertyName("parameters")] public List<SwaggerParameter> Parameters { get; set; } = new();

    [JsonPropertyName("requestBody")] public SwaggerRequestBody? RequestBody { get; set; }
}