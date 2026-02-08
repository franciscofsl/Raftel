# GitHub Copilot Instructions for Raftel

This document serves as the central index for all coding instructions and guidelines in the Raftel project.

## Layer-Specific Instructions

These instruction files provide detailed guidance for writing code in each architectural layer:

### 🏛️ Domain Layer
**File**: `.github/instructions/domain-layer.instructions.md`  
**Applies to**: `**/*Domain*/**/*.cs`

Guidelines for:
- Aggregate roots and entities
- Value objects
- Strongly-typed IDs
- Repository interfaces
- Domain services and specifications
- Business logic encapsulation
- Domain validation

### 🎯 Application Layer
**File**: `.github/instructions/application-layer.instructions.md`  
**Applies to**: `**/*Application*/**/*.cs`

Guidelines for:
- Commands and queries (CQRS)
- Command and query handlers
- Use case orchestration
- DTOs and mapping
- Application validation
- Permission-based authorization

### 🔧 Infrastructure Layer
**File**: `.github/instructions/infrastructure-layer.instructions.md`  
**Applies to**: `**/*Infrastructure*/**/*.cs`

Guidelines for:
- Repository implementations
- EF Core configurations
- Database context
- Entity type configurations
- Dependency injection
- External service integration

### 🌐 API Layer
**File**: `.github/instructions/api-layer.instructions.md`  
**Applies to**: `**/*Api*/**/*.cs`

Guidelines for:
- Auto-endpoint pattern
- Minimal API endpoints
- RESTful URI design
- Request/Response models
- Error handling
- OpenAPI/Swagger documentation

## Architecture and Design Principles

Raftel follows **Clean Architecture** and **Domain-Driven Design (DDD)** principles:

### Clean Architecture
- **Domain Layer**: Core business logic, completely independent
- **Application Layer**: Use cases, orchestrates domain operations
- **Infrastructure Layer**: Technical details, database, external services
- **API Layer**: Exposes application features via HTTP

### Key Principles
1. **Dependency Rule**: Dependencies point inward (API → Application → Domain)
2. **Separation of Concerns**: Each layer has a clear responsibility
3. **Domain-Centric**: Business logic lives in the domain, not scattered across layers
4. **Testability**: Each layer can be tested independently

## CQRS Pattern

Raftel uses **CQRS (Command Query Responsibility Segregation)**:

- **Commands**: Write operations that change state (return `Result` or `Result<T>`)
- **Queries**: Read operations that return data (return `Result<TResponse>`)
- **Handlers**: Process commands and queries
- **Dispatchers**: Route commands/queries to appropriate handlers

## Strongly-Typed IDs

**Always** use strongly-typed IDs instead of primitive types:

```csharp
// ✅ Good
public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value) { }
    public static PirateId New() => new(NewGuid());
}

// ❌ Bad
public Guid PirateId { get; set; }
```

## Value Objects

Use immutable value objects for domain concepts:

```csharp
// ✅ Good
public readonly record struct Name
{
    private readonly string _value;
    
    public Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty.");
        _value = value;
    }
    
    public static implicit operator string(Name name) => name._value;
    public static implicit operator Name(string value) => new(value);
}
```

## Result Pattern

Use the `Result` type for methods that can fail:

```csharp
public Result<Pirate> EatFruit(DevilFruit fruit)
{
    if (!CanEatFruit())
    {
        return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
    }
    
    _eatenDevilFruits.Add(fruit);
    return Result.Success();
}
```

## Testing

- **Unit Tests**: Test business logic in the domain layer
- **Integration Tests**: Test application layer with in-memory database
- **Functional Tests**: Test API endpoints end-to-end

## Code Style

- Use **C# 12+** features (primary constructors, record types, pattern matching)
- Follow **object-oriented principles** and **object calisthenics**
- Write **clean, readable code** with meaningful names
- Use **explicit over implicit** - be clear about intentions
- Keep methods **small and focused** (Single Responsibility Principle)

## Project Structure

```
Raftel/
├── src/
│   ├── Raftel.Domain/          # Core business logic
│   ├── Raftel.Application/     # Use cases and orchestration
│   ├── Raftel.Infrastructure/  # Data access and external services
│   └── Raftel.Api.Server/      # HTTP API endpoints
├── tests/
│   ├── Raftel.Domain.Tests/
│   ├── Raftel.Application.UnitTests/
│   ├── Raftel.Infrastructure.Tests/
│   └── Raftel.Api.FunctionalTests/
└── demo/                       # Example implementation
```

## Getting Started

1. Read the layer-specific instructions for the layer you're working on
2. Look at existing examples in the `demo/` folder
3. Follow the patterns established in the codebase
4. When in doubt, ask or refer to the instruction files

## Additional Resources

- **README.md**: Project overview and getting started guide
- **docs/**: Detailed documentation
- **demo/**: Reference implementation with examples

---

**Note**: These instructions are living documents. If you notice patterns that should be documented or clarified, please contribute to these files.
