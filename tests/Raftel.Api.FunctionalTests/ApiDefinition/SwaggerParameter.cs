using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.ApiDefinition;

public class SwaggerParameter
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("in")] public string In { get; set; } = string.Empty;

    [JsonPropertyName("schema")] public SwaggerSchema Schema { get; set; } = new();
}