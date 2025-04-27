using System.Text.Json.Serialization;

namespace Raftel.Api.Integration.Tests;

public class SwaggerRequestBody
{
    [JsonPropertyName("content")] public Dictionary<string, SwaggerContent> Content { get; set; } = new();
}