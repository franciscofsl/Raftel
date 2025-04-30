using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests;

public class SwaggerRequestBody
{
    [JsonPropertyName("content")] public Dictionary<string, SwaggerContent> Content { get; set; } = new();
}