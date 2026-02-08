---
applyTo: "**/*Api*/**/*.cs"
---

# API Layer Coding Instructions

## Overview

The API layer **exposes** application features through HTTP endpoints. In Raftel, this is implemented using **Minimal APIs** with an auto-endpoint pattern that automatically maps commands and queries to HTTP endpoints.

## 🎯 What Belongs in the API Layer

✅ **DO include:**
- Endpoint definitions and routing
- Request/Response models (when different from application DTOs)
- API versioning configuration
- OpenAPI/Swagger documentation
- Authentication/Authorization middleware configuration
- Error handling and problem details
- Auto-endpoint configurations

❌ **DO NOT include:**
- Business logic (belongs in Domain)
- Use case orchestration (belongs in Application)
- Data access logic (belongs in Infrastructure)
- Direct database queries

## 📁 File and Folder Structure

```
Raftel.Api.Server/
├── Features/
│   └── [FeatureName]/
│       ├── [FeatureName]DependencyInjection.cs
│       └── [FeatureName]Controller.cs (if needed)
├── AutoEndpoints/
│   ├── EndpointRouteBuilderExtensions.cs
│   ├── AutoEndpointGroupExtensions.cs
│   ├── CommandEndpointMapper.cs
│   └── QueryEndpointMapper.cs
└── Program.cs
```

## 🚀 Auto-Endpoint Pattern

Raftel uses an **auto-endpoint pattern** that automatically creates HTTP endpoints for commands and queries.

### Key Principles

1. **Use `AddEndpointGroup`** to define a group of related endpoints
2. **Use `AddCommand<T>`** to map commands to POST endpoints
3. **Use `AddQuery<TQuery, TResponse>`** to map queries to GET endpoints
4. **Group endpoints** by feature/aggregate
5. **Follow RESTful conventions** for URI structure

### ✅ Good Example

```csharp
using Microsoft.AspNetCore.Routing;
using Raftel.Api.Server.AutoEndpoints;

namespace YourApp.Api.Server.Features.Pirates;

public static class PiratesDependencyInjection
{
    public static IEndpointRouteBuilder AddPirateEndpoints(this IEndpointRouteBuilder app)
    {
        app.AddEndpointGroup(group =>
        {
            group.Name = "Pirates";
            group.BaseUri = "/api/pirates";
            
            // Commands (POST)
            group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
            group.AddCommand<UpdatePirateBountyCommand>("{id}/bounty", HttpMethod.Put);
            group.AddCommand<EatDevilFruitCommand>("{id}/eat-fruit", HttpMethod.Post);
            group.AddCommand<DeletePirateCommand>("{id}", HttpMethod.Delete);
            
            // Queries (GET)
            group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);
            group.AddQuery<GetAllPiratesQuery, GetAllPiratesResponse>("", HttpMethod.Get);
            group.AddQuery<GetPiratesByCrewQuery, GetPiratesByCrewResponse>("crew/{crewId}", HttpMethod.Get);
        });

        return app;
    }
}
```

### Register in Program.cs

```csharp
// In Program.cs or similar startup file
app.AddPirateEndpoints();
```

### ❌ Bad Example

```csharp
// DON'T manually create endpoints when auto-endpoints can be used
public static class PiratesDependencyInjection
{
    public static IEndpointRouteBuilder AddPirateEndpoints(this IEndpointRouteBuilder app)
    {
        // ❌ Manual endpoint creation when auto-endpoint pattern should be used
        app.MapPost("/api/pirates", async (
            CreatePirateCommand command,
            ICommandDispatcher dispatcher) =>
        {
            var result = await dispatcher.DispatchAsync(command);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });
        
        return app;
    }
}
```

## 🌐 Endpoint Naming and URI Structure

Follow RESTful conventions for endpoint URIs.

### Key Principles

1. **Use plural nouns** for resources (e.g., `/pirates`, not `/pirate`)
2. **Use lowercase** with hyphens for multi-word paths (e.g., `/devil-fruits`)
3. **Nest resources** logically (e.g., `/pirates/{id}/crew`)
4. **Use HTTP methods semantically**:
   - `POST` for creating resources
   - `GET` for retrieving resources
   - `PUT` for full updates
   - `PATCH` for partial updates
   - `DELETE` for deletion

### ✅ Good Example

```csharp
group.BaseUri = "/api/pirates";

// POST /api/pirates - Create a new pirate
group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);

// GET /api/pirates/{id} - Get a pirate by ID
group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);

// GET /api/pirates - Get all pirates
group.AddQuery<GetAllPiratesQuery, GetAllPiratesResponse>("", HttpMethod.Get);

// PUT /api/pirates/{id}/bounty - Update pirate bounty
group.AddCommand<UpdatePirateBountyCommand>("{id}/bounty", HttpMethod.Put);

// POST /api/pirates/{id}/eat-fruit - Pirate eats a devil fruit
group.AddCommand<EatDevilFruitCommand>("{id}/eat-fruit", HttpMethod.Post);

// DELETE /api/pirates/{id} - Delete a pirate
group.AddCommand<DeletePirateCommand>("{id}", HttpMethod.Delete);
```

### ❌ Bad Example

```csharp
// DON'T use verbs in URIs
group.AddCommand<CreatePirateCommand>("create-pirate", HttpMethod.Post);  // ❌

// DON'T use camelCase or PascalCase in URIs
group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("getPirate/{id}", HttpMethod.Get);  // ❌

// DON'T use singular nouns
group.BaseUri = "/api/pirate";  // ❌ Should be "/api/pirates"
```

## 🔐 Authentication and Authorization

Authentication and authorization are handled at the **application layer** using the `[RequiresPermission]` attribute.

### ✅ Good Example

```csharp
// In Application layer - Command definition
[RequiresPermission(PiratesPermissions.Management)]
public record CreatePirateCommand(string Name, uint Bounty) : ICommand;

// In API layer - Endpoint automatically enforces permission
group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
// This endpoint will automatically require PiratesPermissions.Management
```

### Additional Endpoint-Level Configuration (if needed)

```csharp
// For custom authorization policies at endpoint level
public static class PiratesDependencyInjection
{
    public static IEndpointRouteBuilder AddPirateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pirates")
            .RequireAuthorization()  // Require authentication for all endpoints
            .WithTags("Pirates");    // Group in Swagger
        
        // ... add commands and queries
        
        return app;
    }
}
```

## 📝 Request/Response Models

Most of the time, you'll use **application DTOs** directly. Create API-specific models only when needed.

### When to Create API-Specific Models

- When the API contract differs from the application DTO
- When you need API-specific validation
- When you need to support multiple API versions

### ✅ Good Example - Using Application DTOs

```csharp
// Application layer
public record CreatePirateCommand(string Name, uint Bounty) : ICommand;

// API layer - reuse the command directly
group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
```

### When API Model Differs from Application

```csharp
// API-specific request model
public record CreatePirateApiRequest(string Name, uint Bounty, string CrewName);

// Manual endpoint when transformation is needed
app.MapPost("/api/pirates", async (
    CreatePirateApiRequest request,
    ICommandDispatcher dispatcher,
    ICrewRepository crewRepository) =>
{
    // Transform API model to application command
    var crew = await crewRepository.GetByNameAsync(request.CrewName);
    var command = new CreatePirateCommand(request.Name, request.Bounty, crew.Id);
    
    var result = await dispatcher.DispatchAsync(command);
    return result.IsSuccess ? Results.Created($"/api/pirates/{result.Value}", result.Value) 
                             : Results.BadRequest(result.Error);
});
```

## ⚠️ Error Handling

Raftel's auto-endpoint pattern handles errors automatically by returning proper HTTP status codes and problem details.

### Automatic Error Handling

- **Success**: Returns `200 OK` or `201 Created`
- **Validation Failure**: Returns `400 Bad Request` with error details
- **Not Found**: Returns `404 Not Found`
- **Unauthorized**: Returns `401 Unauthorized`
- **Forbidden**: Returns `403 Forbidden`

### ✅ Good Example - Let Auto-Endpoints Handle Errors

```csharp
// Just define the endpoint - error handling is automatic
group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);
```

### Custom Error Handling (if needed)

```csharp
// For custom error responses
app.MapPost("/api/pirates/batch", async (
    List<CreatePirateCommand> commands,
    ICommandDispatcher dispatcher) =>
{
    var results = new List<Result>();
    
    foreach (var command in commands)
    {
        results.Add(await dispatcher.DispatchAsync(command));
    }
    
    var failures = results.Where(r => r.IsFailure).ToList();
    
    if (failures.Any())
    {
        return Results.BadRequest(new 
        { 
            errors = failures.Select(f => f.Error) 
        });
    }
    
    return Results.Ok();
});
```

## 📚 OpenAPI/Swagger Documentation

Configure Swagger for API documentation.

### ✅ Good Example

```csharp
// In Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pirates API",
        Version = "v1",
        Description = "API for managing pirates and their crews"
    });
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// After app is built
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pirates API v1");
});
```

## 🧪 Testing API Endpoints

Use functional tests to test API endpoints end-to-end.

### ✅ Good Example

```csharp
public class PirateEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public PirateEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreatePirate_WithValidData_ShouldReturn201()
    {
        // Arrange
        var command = new CreatePirateCommand("Luffy", 1_500_000);
        var content = JsonContent.Create(command);
        
        // Act
        var response = await _client.PostAsync("/api/pirates", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetPirate_WhenExists_ShouldReturn200WithData()
    {
        // Arrange
        var pirateId = await CreateTestPirateAsync();
        
        // Act
        var response = await _client.GetAsync($"/api/pirates/{pirateId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pirate = await response.Content.ReadFromJsonAsync<GetPirateByIdResponse>();
        pirate.Should().NotBeNull();
        pirate!.Name.Should().Be("Luffy");
    }
}
```

## 📚 Naming Conventions

- **Endpoint Groups**: `[Feature]DependencyInjection` (e.g., `PiratesDependencyInjection`)
- **Extension Methods**: `Add[Feature]Endpoints` (e.g., `AddPirateEndpoints`)
- **Base URI**: `/api/[resource-plural]` (e.g., `/api/pirates`, `/api/devil-fruits`)
- **Tags**: Feature name in PascalCase (e.g., `"Pirates"`, `"DevilFruits"`)

## 🎯 Do's and Don'ts

### ✅ DO

- Use the auto-endpoint pattern for standard CRUD operations
- Follow RESTful URI conventions (plural nouns, lowercase with hyphens)
- Use appropriate HTTP methods (POST for commands, GET for queries)
- Group related endpoints together
- Use `[RequiresPermission]` attribute in application layer
- Reuse application DTOs when possible
- Use functional tests for API endpoints
- Document APIs with Swagger/OpenAPI
- Return proper HTTP status codes

### ❌ DON'T

- Put business logic in endpoints
- Create manual endpoints when auto-endpoints can be used
- Use verbs in URIs (e.g., `/api/create-pirate`)
- Use camelCase or PascalCase in URIs
- Bypass application layer and call repositories directly
- Expose domain entities directly through the API
- Return custom status codes without good reason
- Forget to add authorization where needed

## 🔗 Related Instructions

- Application Layer instructions (for commands, queries, and handlers)
- Clean Architecture principles
- RESTful API design guidelines
- C# Coding Style guidelines

## 📖 Further Reading

See existing examples in:
- `src/Raftel.Api.Server/Features/Users/` - User endpoints
- `src/Raftel.Api.Server/Features/Tenants/` - Tenant endpoints
- `src/Raftel.Api.Server/AutoEndpoints/` - Auto-endpoint implementation
- `tests/Raftel.Api.FunctionalTests/` - Functional test examples
