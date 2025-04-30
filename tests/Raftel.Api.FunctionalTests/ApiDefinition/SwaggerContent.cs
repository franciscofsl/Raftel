using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.ApiDefinition;

public class SwaggerContent
{
    [JsonPropertyName("schema")] public SwaggerSchema Schema { get; set; } = new();
}