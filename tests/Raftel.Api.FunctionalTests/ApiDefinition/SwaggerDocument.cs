using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.ApiDefinition;

public class SwaggerDocument
{
    [JsonPropertyName("paths")]
    public Dictionary<string, Dictionary<string, SwaggerPathMethod>> Paths { get; set; } = new();
}