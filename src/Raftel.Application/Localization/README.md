# Localization System

This folder contains the core localization and translation infrastructure for the Raftel framework.

## Components

### Core Interfaces

- **`ILocalizationService`**: Main service interface for accessing localized strings
- **`IResourceProvider`**: Interface for loading localization resources from different sources

### Implementations

- **`LocalizationService`**: Main service implementation with caching support
- **`JsonResourceProvider`**: Loads localization resources from JSON files
- **`RaftelStringLocalizer<T>`**: Custom implementation of `IStringLocalizer<T>` for .NET integration
- **`RaftelStringLocalizerFactory`**: Factory for creating `IStringLocalizer` instances

### Configuration

- **`LocalizationOptions`**: Configuration options for the localization system
- **`LocalizationExtensions`**: Extension methods for registering localization services

### Models

- **`LocalizationResource`**: Represents a collection of translations for a specific culture

## Usage

See the [Localization Documentation](../../docs/localization.md) for detailed usage instructions and examples.

## Architecture

The localization system follows these principles:

1. **Module-based Organization**: Each module has its own folder with culture-specific JSON files
2. **Provider Pattern**: Uses `IResourceProvider` to abstract the source of translations
3. **Caching**: Built-in memory caching for performance
4. **Fallback**: Automatic fallback to default culture when translations are missing
5. **Standard Integration**: Implements .NET's `IStringLocalizer<T>` interface

## Adding New Resource Providers

To add a new resource provider (e.g., database-based):

1. Implement `IResourceProvider` interface
2. Register your provider in the DI container
3. The localization service will use your provider automatically

Example:

```csharp
public class DatabaseResourceProvider : IResourceProvider
{
    public async Task<LocalizationResource?> LoadResourceAsync(string moduleName, string culture)
    {
        // Load from database
    }

    public async Task<IEnumerable<string>> GetAvailableModulesAsync()
    {
        // Return modules from database
    }
}
```

Register:

```csharp
builder.Services.AddSingleton<IResourceProvider, DatabaseResourceProvider>();
```
