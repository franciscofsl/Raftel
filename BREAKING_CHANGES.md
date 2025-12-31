# Breaking Changes - .NET 10 Migration

This document describes the breaking changes introduced during the migration from .NET 9 to .NET 10.

## Framework Update

### .NET 10 Target Framework
**What changed:** All projects now target .NET 10 instead of .NET 9.

**Why:** To adopt the latest framework version and benefit from new features, performance improvements, and security updates.

**Impact:** Consumers must upgrade to .NET 10 SDK to build and run the library.

**Action required:** 
- Install [.NET 10 SDK](https://dotnet.microsoft.com/download) (version 10.0 or higher)
- Update your project's `TargetFramework` to `net10.0` if you're consuming this library

---

## NuGet Package Updates

### Microsoft.OpenApi 2.0.0
**What changed:** Updated from Microsoft.OpenApi 1.6.x to 2.0.0

**Why:** Required for compatibility with .NET 10's `Microsoft.AspNetCore.OpenApi 10.0.1` package.

**Impact:** The `Microsoft.OpenApi.Models` namespace has been removed. All OpenApi types are now in the `Microsoft.OpenApi` namespace directly.

**Action required:**
If you're using the Raftel.Api.Server package and customizing OpenAPI configurations:
- Change `using Microsoft.OpenApi.Models;` to `using Microsoft.OpenApi;`
- `OpenApiSchema.Type` now uses `JsonSchemaType` enum instead of string
- Update custom OpenAPI filters and configurations according to [Microsoft.OpenApi 2.0 migration guide](https://github.com/dotnet/aspnetcore/issues/61123)

**Example:**
```csharp
// Before (.NET 9)
using Microsoft.OpenApi.Models;

var schema = new OpenApiSchema 
{ 
    Type = "string",
    Format = "uuid"
};

// After (.NET 10)
using Microsoft.OpenApi;

var schema = new OpenApiSchema 
{ 
    Type = JsonSchemaType.String,
    Format = "uuid"
};
```

### Swashbuckle.AspNetCore 10.1.0
**What changed:** Updated from Swashbuckle.AspNetCore 8.1.1 to 10.1.0

**Why:** Required for compatibility with Microsoft.OpenApi 2.0.0

**Impact:** If you're using Swashbuckle in your application that consumes this library, you may need to update custom Swagger filters and configurations.

**Action required:**
- Update Swashbuckle.AspNetCore to version 10.1.0 or higher
- Review and update custom Swagger filters according to [Swashbuckle v10 migration guide](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md)

### Entity Framework Core 10.0.1
**What changed:** Updated all Entity Framework Core packages from 9.0.4 to 10.0.1

**Why:** Required for .NET 10 compatibility

**Impact:** No breaking changes expected in normal usage.

**Action required:** Update your EF Core packages to 10.0.1 or higher if you're using Entity Framework.

### Microsoft.Extensions.* Packages 10.0.1
**What changed:** Updated all Microsoft.Extensions packages from 9.0.x to 10.0.1

**Why:** Required for .NET 10 compatibility

**Impact:** No breaking changes expected in normal usage.

**Action required:** Update your Microsoft.Extensions packages to 10.0.1 or higher.

---

## Deprecated APIs

### WithOpenApi Extension Method
**What changed:** The `WithOpenApi` extension method is now marked as obsolete in .NET 10.

**Why:** Microsoft is moving towards a new OpenAPI generation approach.

**Impact:** You'll see deprecation warnings (ASPDEPR002) when using `WithOpenApi`.

**Action required:** 
- The method still works in .NET 10 but will be removed in a future version
- Plan to migrate to the new OpenAPI generation APIs when they become available
- For now, you can suppress the warning or continue using it until a replacement is provided

---

## Summary

The migration to .NET 10 is primarily focused on framework and dependency updates. The main breaking change that may affect consumers is the Microsoft.OpenApi 2.0 update, which requires namespace and type changes if you're customizing OpenAPI/Swagger configurations.

All tests pass successfully after migration, confirming that the functional behavior of the library remains unchanged.
