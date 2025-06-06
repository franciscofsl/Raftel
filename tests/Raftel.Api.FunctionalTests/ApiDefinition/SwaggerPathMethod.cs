﻿using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.ApiDefinition;

public class SwaggerPathMethod
{
    [JsonPropertyName("parameters")] public List<SwaggerParameter> Parameters { get; set; } = new();

    [JsonPropertyName("requestBody")] public SwaggerRequestBody? RequestBody { get; set; }
}