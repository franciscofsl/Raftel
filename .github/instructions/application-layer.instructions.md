---
applyTo: "**/*Application*/**/*.cs"
---

# Application Layer Coding Instructions

## Overview

The Application layer **orchestrates** the execution of use cases by coordinating the domain layer and infrastructure services. It contains commands, queries, handlers, and application-specific logic.

## 🎯 What Belongs in the Application Layer

✅ **DO include:**
- Commands and Command Handlers
- Queries and Query Handlers
- Application Services
- DTOs (Data Transfer Objects)
- Request/Response models
- Validators (using `Validator<T>`)
- Application-specific exceptions
- Use case orchestration logic
- Permission definitions
- Mapping logic (domain ↔ DTOs)

❌ **DO NOT include:**
- Business rules (those belong in Domain)
- Database access logic (belongs in Infrastructure)
- HTTP/API concerns (belongs in API layer)
- Infrastructure implementation details
- Direct entity manipulation that bypasses domain methods

## 📁 File and Folder Structure

```
Raftel.Application/
├── Features/
│   └── [FeatureName]/
│       ├── [Action]Command.cs
│       ├── [Action]CommandHandler.cs
│       ├── [Action]CommandValidator.cs
│       ├── [Query]Query.cs
│       ├── [Query]QueryHandler.cs
│       ├── [FeatureName]Response.cs
│       └── [FeatureName]Permissions.cs
├── Commands/
│   ├── ICommand.cs
│   └── ICommandHandler.cs
├── Queries/
│   ├── IQuery.cs
│   └── IQueryHandler.cs
└── Middlewares/
    └── [Middleware]Middleware.cs
```

## 📝 Commands

Commands represent **write operations** that change the state of the system. They follow the **Command pattern** and CQRS principles.

### Key Principles

1. **Use `record` types** for immutability
2. **Implement `ICommand`** (for void return) or `ICommand<TResponse>` (for typed return)
3. **Use `[RequiresPermission]`** attribute for authorization
4. **Keep commands simple** - they are just data containers
5. **Name commands with verbs** (e.g., `CreatePirateCommand`, `UpdateBountyCommand`)

### ✅ Good Example

```csharp
using Raftel.Application.Authorization;
using Raftel.Application.Commands;

namespace YourApp.Application.Pirates.CreatePirate;

[RequiresPermission(PiratesPermissions.Management)]
public record CreatePirateCommand(string Name, uint Bounty, bool IsKing = false) : ICommand;
```

### ❌ Bad Example

```csharp
// DON'T use mutable classes
public class CreatePirateCommand : ICommand
{
    public string Name { get; set; }  // ❌ Mutable
    public uint Bounty { get; set; }  // ❌ Mutable
}

// DON'T include business logic in commands
public record CreatePirateCommand(string Name, uint Bounty) : ICommand
{
    public bool IsValid() => !string.IsNullOrEmpty(Name);  // ❌ Use validators
}
```

## 🔧 Command Handlers

Command handlers **execute commands** and orchestrate domain operations.

### Key Principles

1. **Implement `ICommandHandler<TCommand>`** or `ICommandHandler<TCommand, TResponse>`**
2. **Use constructor injection** for dependencies
3. **Return `Result` or `Result<T>`** to indicate success/failure
4. **Keep handlers thin** - delegate to domain for business logic
5. **Handle one command per handler** (Single Responsibility)

### ✅ Good Example

```csharp
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace YourApp.Application.Pirates.CreatePirate;

public sealed class CreatePirateCommandHandler(IPirateRepository repository) 
    : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(
        CreatePirateCommand request, 
        CancellationToken token = default)
    {
        // Use domain factory method
        var pirate = Pirate.Normal(request.Name, request.Bounty);
        
        // If the king flag is set, call domain method
        if (request.IsKing)
        {
            pirate.FoundOnePiece();
        }
        
        // Persist using repository
        await repository.AddAsync(pirate, token);
        
        return Result.Success();
    }
}
```

### ❌ Bad Example

```csharp
// DON'T put business logic in handlers
public class CreatePirateCommandHandler : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand cmd, CancellationToken token)
    {
        // ❌ Business logic should be in domain
        if (cmd.IsKing && cmd.Name != "Luffy")
        {
            return Result.Failure("Only Luffy can be king");
        }
        
        // ❌ Direct instantiation instead of factory method
        var pirate = new Pirate 
        { 
            Id = PirateId.New(),
            Name = cmd.Name,
            Bounty = cmd.Bounty 
        };
        
        await repository.AddAsync(pirate, token);
        return Result.Success();
    }
}
```

## 🔍 Queries

Queries represent **read operations** that return data without changing state.

### Key Principles

1. **Use `record` types** for immutability
2. **Implement `IQuery<TResponse>`**
3. **Use `[RequiresPermission]`** attribute for authorization
4. **Name queries with "Get"** (e.g., `GetPirateByIdQuery`, `GetAllPiratesQuery`)

### ✅ Good Example

```csharp
using Raftel.Application.Queries;

namespace YourApp.Application.Pirates.GetPirateById;

[RequiresPermission(PiratesPermissions.View)]
public sealed record GetPirateByIdQuery(Guid Id) : IQuery<GetPirateByIdResponse>;
```

### Response DTOs

```csharp
namespace YourApp.Application.Pirates.GetPirateById;

public sealed record GetPirateByIdResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public uint Bounty { get; init; }
    public bool IsKing { get; init; }
}
```

## 🔎 Query Handlers

Query handlers **execute queries** and return data.

### Key Principles

1. **Implement `IQueryHandler<TQuery, TResponse>`**
2. **Return `Result<TResponse>`** to handle not found scenarios
3. **Map domain entities to DTOs** - never return domain entities directly
4. **Keep queries optimized** - only fetch what you need

### ✅ Good Example

```csharp
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace YourApp.Application.Pirates.GetPirateById;

internal sealed class GetPirateByIdQueryHandler(IPirateRepository repository)
    : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public async Task<Result<GetPirateByIdResponse>> HandleAsync(
        GetPirateByIdQuery request, 
        CancellationToken token = default)
    {
        var pirate = await repository.GetByIdAsync(new PirateId(request.Id), token);
        
        if (pirate is null)
        {
            return Result.Failure<GetPirateByIdResponse>(
                new Error("Pirate.NotFound", "Pirate not found"));
        }

        // Map to DTO
        return new GetPirateByIdResponse
        {
            Id = pirate.Id,
            Name = pirate.Name,
            Bounty = pirate.Bounty,
            IsKing = pirate.IsKing
        };
    }
}
```

### ❌ Bad Example

```csharp
// DON'T return domain entities directly
public class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, Pirate>
{
    public async Task<Result<Pirate>> HandleAsync(GetPirateByIdQuery query, CancellationToken token)
    {
        return await repository.GetByIdAsync(new PirateId(query.Id), token);  // ❌
    }
}

// DON'T swallow errors with null
public class GetPirateByIdQueryHandler : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public async Task<Result<GetPirateByIdResponse>> HandleAsync(...)
    {
        var pirate = await repository.GetByIdAsync(new PirateId(request.Id), token);
        
        // ❌ Returning null instead of proper error
        return pirate is null ? null : MapToDto(pirate);
    }
}
```

## ✅ Validation

Use the `Validator<T>` base class for command and query validation.

### Key Principles

1. **Create validator classes** that inherit from `Validator<T>`
2. **Use `EnsureThat`** for validation rules
3. **Return domain errors** for validation failures
4. **Validate input data**, not business rules (those are in domain)

### ✅ Good Example

```csharp
using Raftel.Domain.Validators;

namespace YourApp.Application.Pirates.CreatePirate;

public class CreatePirateCommandValidator : Validator<CreatePirateCommand>
{
    public CreatePirateCommandValidator()
    {
        EnsureThat(cmd => !string.IsNullOrWhiteSpace(cmd.Name), 
            CreatePirateErrors.NameRequired);
            
        EnsureThat(cmd => cmd.Bounty > 0, 
            CreatePirateErrors.BountyMustBePositive);
            
        // Domain-specific business rule validation
        EnsureThat(cmd => !cmd.IsKing || cmd.Name == "Luffy", 
            CreatePirateErrors.KingMustBeLuffy);
    }
}
```

### Define Validation Errors

```csharp
using Raftel.Domain.Abstractions;

namespace YourApp.Application.Pirates.CreatePirate;

public static class CreatePirateErrors
{
    public static readonly Error NameRequired = 
        new("CreatePirate.NameRequired", "Pirate name is required");
    
    public static readonly Error BountyMustBePositive = 
        new("CreatePirate.BountyMustBePositive", "Bounty must be greater than zero");
    
    public static readonly Error KingMustBeLuffy = 
        new("CreatePirate.KingMustBeLuffy", "Only Luffy can be the Pirate King");
}
```

### ❌ Bad Example

```csharp
// DON'T use exception-based validation
public class CreatePirateCommandValidator : Validator<CreatePirateCommand>
{
    public CreatePirateCommandValidator()
    {
        // ❌ Don't throw exceptions in validators
        if (string.IsNullOrEmpty(cmd.Name))
            throw new ValidationException("Name required");
    }
}

// DON'T use attribute-based validation (like FluentValidation)
// Use Raftel's Validator<T> pattern instead
```

## 🔐 Authorization

Use the `[RequiresPermission]` attribute to enforce authorization.

### ✅ Good Example

```csharp
namespace YourApp.Application.Pirates;

public static class PiratesPermissions
{
    public const string View = "Pirates.View";
    public const string Management = "Pirates.Management";
}

// On commands
[RequiresPermission(PiratesPermissions.Management)]
public record CreatePirateCommand(string Name, uint Bounty) : ICommand;

// On queries
[RequiresPermission(PiratesPermissions.View)]
public record GetPirateByIdQuery(Guid Id) : IQuery<GetPirateByIdResponse>;
```

## 🔄 Use Case Orchestration

Handlers should orchestrate the use case by coordinating multiple domain operations.

### ✅ Good Example

```csharp
public sealed class EatDevilFruitCommandHandler(
    IPirateRepository pirateRepository,
    IDevilFruitRepository fruitRepository)
    : ICommandHandler<EatDevilFruitCommand>
{
    public async Task<Result> HandleAsync(
        EatDevilFruitCommand request, 
        CancellationToken token = default)
    {
        // 1. Fetch required aggregates
        var pirate = await pirateRepository.GetByIdAsync(
            new PirateId(request.PirateId), token);
        
        if (pirate is null)
        {
            return Result.Failure(PirateErrors.NotFound);
        }
        
        var fruit = await fruitRepository.GetByIdAsync(
            new DevilFruitId(request.FruitId), token);
        
        if (fruit is null)
        {
            return Result.Failure(DevilFruitErrors.NotFound);
        }
        
        // 2. Execute domain logic
        var eatResult = pirate.EatFruit(fruit);
        
        if (eatResult.IsFailure)
        {
            return eatResult;
        }
        
        // 3. Persist changes
        pirateRepository.Update(pirate);
        
        return Result.Success();
    }
}
```

## 🧪 Testing Application Logic

Application layer tests should focus on **use case orchestration**, not business rules (those are tested in domain tests).

### ✅ Good Example

```csharp
public class CreatePirateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreatePirate()
    {
        // Arrange
        var repository = Substitute.For<IPirateRepository>();
        var handler = new CreatePirateCommandHandler(repository);
        var command = new CreatePirateCommand("Luffy", 1_500_000);
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<Pirate>(p => p.Name == "Luffy" && p.Bounty == 1_500_000),
            Arg.Any<CancellationToken>());
    }
}
```

## 📚 Naming Conventions

- **Commands**: `[Action][Entity]Command` (e.g., `CreatePirateCommand`, `UpdateBountyCommand`)
- **Command Handlers**: `[CommandName]Handler` (e.g., `CreatePirateCommandHandler`)
- **Queries**: `Get[Entity][Filter]Query` (e.g., `GetPirateByIdQuery`, `GetAllPiratesQuery`)
- **Query Handlers**: `[QueryName]Handler` (e.g., `GetPirateByIdQueryHandler`)
- **Response DTOs**: `[QueryName]Response` (e.g., `GetPirateByIdResponse`)
- **Validators**: `[CommandOrQuery]Validator` (e.g., `CreatePirateCommandValidator`)
- **Permissions**: `[Feature]Permissions` (e.g., `PiratesPermissions`)

## 🎯 Do's and Don'ts

### ✅ DO

- Use `record` types for commands and queries
- Return `Result` or `Result<T>` from handlers
- Map domain entities to DTOs in query handlers
- Use `[RequiresPermission]` for authorization
- Keep handlers thin - delegate to domain
- Use dependency injection
- Validate input data with `Validator<T>`
- Use `internal sealed` for handler implementations
- Name commands with verbs (Create, Update, Delete)
- Name queries with "Get"

### ❌ DON'T

- Put business logic in handlers - that belongs in domain
- Return domain entities from queries - use DTOs
- Create mutable command/query classes - use records
- Bypass domain methods by manipulating entities directly
- Use exceptions for validation - use `Validator<T>` with `Result`
- Reference Infrastructure or API layers
- Add CRUD methods to handlers - keep them focused on use cases
- Make handlers public - keep them `internal sealed`

## 🔗 Related Instructions

- Domain Layer instructions (for business logic patterns)
- Clean Architecture principles
- CQRS pattern guidance
- C# Coding Style guidelines

## 📖 Further Reading

See existing examples in:
- `src/Raftel.Application/Features/Tenants/` - Tenant use cases
- `src/Raftel.Application/Features/Users/` - User use cases
- `demo/Raftel.Demo.Application/Pirates/` - Pirate use cases (comprehensive examples)
