﻿using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.ApiDefinition;

public class SwaggerSchema
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("format")] public string? Format { get; set; }

    [JsonPropertyName("$ref")] public string? Ref { get; set; }
}