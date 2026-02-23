# Localization and Translation System

The Raftel framework provides a comprehensive localization and translation system that allows you to organize translations by modules in JSON files and serve them dynamically based on the requested culture.

## Features

- ✅ **Module-based organization**: Organize translations by modules in JSON files
- ✅ **Dynamic culture detection**: Automatically detect culture from HTTP headers, query parameters, or cookies
- ✅ **IStringLocalizer integration**: Native integration with .NET's `IStringLocalizer<T>`
- ✅ **Fallback support**: Automatic fallback to default culture when translations are missing
- ✅ **Performance caching**: Built-in caching for optimal performance
- ✅ **REST API endpoints**: Expose translations through REST API for client-side applications
- ✅ **Format arguments**: Support for parameterized translations

## Quick Start

### 1. Configure Localization Services

In your `Program.cs` or startup configuration, add the localization services:

```csharp
using Raftel.Application.Localization;

// Add localization services
builder.Services.AddRaftelLocalization(options =>
{
    options.DefaultCulture = "en";
    options.SupportedCultures = new List<string> { "en", "es", "fr" };
    options.EnableCaching = true;
}, AppDomain.CurrentDomain.BaseDirectory);
```

### 2. Add Localization Middleware

Add the localization middleware to your application pipeline:

```csharp
using Raftel.Api.Server.Features.Localization;

// Add localization middleware
app.AddRaftelLocalization();
```

### 3. Create Localization Files

Create a folder structure for your localization files:

```
/Localization
  /Pirates
    - es.json
    - en.json
    - fr.json
  /Ships
    - es.json
    - en.json
  /Common
    - es.json
    - en.json
```

Example JSON file (`/Localization/Pirates/en.json`):

```json
{
  "culture": "en",
  "texts": {
    "PirateNotFound": "Pirate not found",
    "PirateCreated": "Pirate created successfully",
    "Welcome": "Welcome {0}"
  }
}
```

Example Spanish translation (`/Localization/Pirates/es.json`):

```json
{
  "culture": "es",
  "texts": {
    "PirateNotFound": "Pirata no encontrado",
    "PirateCreated": "Pirata creado exitosamente",
    "Welcome": "Bienvenido {0}"
  }
}
```

### 4. Ensure JSON Files are Copied to Output

Update your `.csproj` file to copy the localization files to the output directory:

```xml
<ItemGroup>
    <None Update="Localization\**\*.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

## Using Localization in Code

### Using IStringLocalizer<T>

Create a marker class for your module:

```csharp
namespace MyApp.Pirates;

public class PiratesModule { }
```

Inject `IStringLocalizer<T>` in your service or handler:

```csharp
using Microsoft.Extensions.Localization;

public class GetPirateByIdQueryHandler
{
    private readonly IStringLocalizer<PiratesModule> _localizer;

    public GetPirateByIdQueryHandler(IStringLocalizer<PiratesModule> localizer)
    {
        _localizer = localizer;
    }

    public async Task<Result> HandleAsync(Query request)
    {
        if (pirate is null)
        {
            // Get localized error message
            var errorMessage = _localizer["PirateNotFound"].Value;
            return Result.Failure(new Error("PirateNotFound", errorMessage));
        }
        
        // With format arguments
        var welcome = _localizer["Welcome", userName].Value;
        
        return Result.Success();
    }
}
```

### Using ILocalizationService Directly

For more control, you can inject `ILocalizationService`:

```csharp
using Raftel.Application.Localization;

public class MyService
{
    private readonly ILocalizationService _localizationService;

    public MyService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string GetTranslation()
    {
        // Get translation for specific culture
        var text = _localizationService.GetString("PirateNotFound", "es", "Pirates");
        
        // With format arguments
        var welcome = _localizationService.GetString("Welcome", "es", new[] { "Luffy" }, "Pirates");
        
        return text;
    }
}
```

## REST API Endpoints

The localization system exposes REST API endpoints for retrieving translations:

### Get Resources by Culture and Modules

```http
GET /api/localization/resources?cultureName=es&modules=Pirates,Ships
```

**Response:**
```json
{
  "culture": "es",
  "texts": {
    "PirateNotFound": "Pirata no encontrado",
    "ShipNotFound": "Barco no encontrado"
  }
}
```

### Get Resources by Culture (All Modules)

```http
GET /api/localization/resources/{cultureName}
```

Example:
```http
GET /api/localization/resources/fr
```

**Response:**
```json
{
  "culture": "fr",
  "texts": {
    "PirateNotFound": "Pirate non trouvé",
    "PirateCreated": "Pirate créé avec succès"
  }
}
```

## Culture Detection

The localization middleware detects the current culture in the following order:

1. **Query Parameter**: `?culture=es`
2. **Cookie**: `culture` cookie
3. **Accept-Language Header**: Uses the first supported language from the header
4. **Default Culture**: Falls back to the configured default culture

### Examples

#### Using Query Parameter
```http
GET /api/pirates/1?culture=es
```

#### Using Header
```http
GET /api/pirates/1
Accept-Language: es,en;q=0.9
```

#### Using Cookie
Set a cookie named `culture` with the desired culture code.

## Configuration Options

### LocalizationOptions

```csharp
public class LocalizationOptions
{
    // Default culture to use when no culture is specified (default: "en")
    public string DefaultCulture { get; set; } = "en";

    // List of supported cultures
    public List<string> SupportedCultures { get; set; } = new() { "en", "es" };

    // Base path for localization resources (default: "Localization")
    public string ResourcesPath { get; set; } = "Localization";

    // Enable caching of localization resources (default: true)
    public bool EnableCaching { get; set; } = true;

    // Use fallback to default culture when a key is not found (default: true)
    public bool UseFallbackCulture { get; set; } = true;
}
```

### Example Configuration

```csharp
builder.Services.AddRaftelLocalization(options =>
{
    options.DefaultCulture = "en";
    options.SupportedCultures = new List<string> { "en", "es", "fr", "de" };
    options.ResourcesPath = "Resources/Localization";
    options.EnableCaching = true;
    options.UseFallbackCulture = true;
}, AppDomain.CurrentDomain.BaseDirectory);
```

## Advanced Features

### Multiple Search Paths

You can specify multiple search paths for localization files:

```csharp
builder.Services.AddRaftelLocalization(
    options => { /* configuration */ },
    AppDomain.CurrentDomain.BaseDirectory,
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdditionalResources")
);
```

### Format Arguments and Interpolation

Localization keys support format arguments:

**JSON:**
```json
{
  "Welcome": "Welcome {0}!",
  "Bounty": "Bounty: {0:N0} berries"
}
```

**C# Code:**
```csharp
var message = _localizer["Welcome", "Luffy"].Value;
// Output: "Welcome Luffy!"

var bounty = _localizer["Bounty", 1500000000].Value;
// Output: "Bounty: 1,500,000,000 berries"
```

### Fallback Behavior

When `UseFallbackCulture` is enabled:

1. If a key is not found in the requested culture, the system tries the default culture
2. If the key is still not found, it returns the key itself
3. This ensures your application never breaks due to missing translations

**Example:**
```csharp
// Request Spanish translation but key doesn't exist in Spanish
var text = _localizationService.GetString("NewKey", "es", "Pirates");
// Falls back to English (default culture)
// If not found there either, returns "NewKey"
```

## Best Practices

1. **Organize by Module**: Keep translations organized by feature/module for better maintainability
2. **Use Marker Classes**: Create a marker class for each module to leverage type-safe `IStringLocalizer<T>`
3. **Consistent Keys**: Use consistent naming conventions for translation keys (e.g., PascalCase)
4. **Include All Cultures**: Ensure all supported cultures have translations for the same keys
5. **Format Arguments**: Use format arguments instead of string concatenation for dynamic content
6. **Default Culture**: Always provide translations in your default culture as a fallback
7. **Cache Enabled**: Keep caching enabled in production for better performance

## Testing

The localization system includes comprehensive tests:

### Unit Tests
- `LocalizationServiceTests`: Tests for the core localization service
- `JsonResourceProviderTests`: Tests for JSON file loading
- `RaftelStringLocalizerTests`: Tests for `IStringLocalizer<T>` implementation

### Integration Tests
- `LocalizationEndpointsTests`: End-to-end tests for REST API endpoints

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~Localization"
```

## Troubleshooting

### Translations Not Loading

1. Verify JSON files are copied to output directory (check `.csproj`)
2. Check that the folder structure matches the configured `ResourcesPath`
3. Ensure JSON files are valid (use a JSON validator)
4. Verify the culture code matches your supported cultures

### Wrong Culture Detected

1. Check the order of culture detection (query → cookie → header → default)
2. Verify the culture code is in the `SupportedCultures` list
3. Clear browser cookies if using cookie-based culture selection

### Keys Not Found

1. Ensure the key exists in the JSON file
2. Check for typos in the key name (keys are case-sensitive)
3. Verify `UseFallbackCulture` is enabled for graceful degradation

## Migration from Other Systems

### From ABP Framework

The Raftel localization system is inspired by ABP Framework and follows similar conventions:

- JSON file structure is compatible
- Module-based organization is the same
- `IStringLocalizer<T>` works the same way

Main differences:
- Raftel uses a simpler configuration
- No database localization provider (file-based only)
- Lighter weight implementation

## See Also

- [ASP.NET Core Localization Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization)
- [IStringLocalizer Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.localization.istringlocalizer)
- [ABP Framework Localization](https://docs.abp.io/en/abp/latest/Localization)
