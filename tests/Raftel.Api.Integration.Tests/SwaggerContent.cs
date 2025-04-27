using System.Text.Json.Serialization;

namespace Raftel.Api.Integration.Tests;

public class SwaggerContent
{
    [JsonPropertyName("schema")] public SwaggerSchema Schema { get; set; } = new();
}