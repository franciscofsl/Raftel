---
applyTo: "**/*Domain*/**/*.cs"
---

# Domain Layer Coding Instructions

## Overview

The Domain layer is the **core** of the application, containing the business logic, entities, value objects, and domain services. It should be **completely independent** of infrastructure concerns and should not reference any other layer except shared utilities.

## 🎯 What Belongs in the Domain Layer

✅ **DO include:**
- Aggregate Roots
- Entities
- Value Objects
- Strongly-typed IDs
- Domain Events
- Repository Interfaces (contracts only)
- Domain Services
- Specifications
- Domain Validators
- Business rules and invariants
- Domain-specific exceptions and errors

❌ **DO NOT include:**
- Database access logic
- External service calls
- Application orchestration logic
- HTTP/API concerns
- UI logic
- Infrastructure dependencies

## 📁 File and Folder Structure

```
Raftel.Domain/
├── Features/
│   └── [FeatureName]/
│       ├── [AggregateName].cs          # Aggregate root
│       ├── I[AggregateName]Repository.cs  # Repository interface
│       ├── [AggregateName]Errors.cs    # Domain errors
│       └── ValueObjects/
│           ├── [AggregateName]Id.cs    # Strongly-typed ID
│           └── [ValueObjectName].cs    # Value objects
├── BaseTypes/
│   ├── AggregateRoot.cs
│   ├── Entity.cs
│   ├── TypedGuidId.cs
│   └── TypedId.cs
├── Specifications/
│   └── [SpecificationName].cs
└── Validators/
    └── Validator.cs
```

## 🔑 Strongly-Typed IDs

**ALWAYS** use strongly-typed IDs for aggregate roots and entities.

### ✅ Good Example

```csharp
// Domain/Features/Pirates/ValueObjects/PirateId.cs
using Raftel.Domain.BaseTypes;

namespace YourApp.Domain.Pirates.ValueObjects;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(NewGuid());
}
```

### ❌ Bad Example

```csharp
// DON'T use primitive types as IDs
public class Pirate
{
    public Guid Id { get; set; }  // ❌ Primitive obsession
}
```

## 🏗️ Aggregate Roots

Aggregate roots are the **entry points** to your domain model. They enforce invariants and control access to their child entities.

### Key Principles

1. **Use private setters** for properties that should only be modified through domain methods
2. **Encapsulate business logic** in methods, not in property setters
3. **Factory methods** for object creation (e.g., `Pirate.Normal()`, `Pirate.Special()`)
4. **Private constructors** with public static factory methods
5. **Invariant enforcement** - ensure the aggregate is always in a valid state

### ✅ Good Example

```csharp
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;

namespace YourApp.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private readonly DevilFruitCollection _eatenDevilFruits;
    private readonly BodyType _bodyType;

    // Private constructor - forces use of factory methods
    private Pirate(Name name, Bounty bounty, BodyType bodyType) : this()
    {
        Name = name;
        Bounty = bounty;
        _bodyType = bodyType;
        _eatenDevilFruits = new DevilFruitCollection();
    }

    private Pirate() : base(PirateId.New())
    {
    }

    // Public read-only properties
    public Name Name { get; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; private set; }

    // Factory methods for different creation scenarios
    public static Pirate Normal(Name name, Bounty bounty) 
        => new(name, bounty, BodyType.Normal);

    public static Pirate Special(Name name, Bounty bounty) 
        => new(name, bounty, BodyType.Special);

    // Business logic encapsulated in methods
    public void FoundOnePiece() => IsKing = true;

    // Methods return Result to indicate success/failure
    public Result EatFruit(DevilFruit fruit)
    {
        if (!CanEatFruit())
        {
            return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
        }

        _eatenDevilFruits.Add(fruit);
        return Result.Success();
    }

    // Private helper methods for invariant checks
    private bool CanEatFruit() 
        => _bodyType != BodyType.Normal || !_eatenDevilFruits.HasAny();
}
```

### ❌ Bad Example

```csharp
// DON'T expose business logic through public setters
public class Pirate : AggregateRoot<PirateId>
{
    public Pirate() : base(PirateId.New()) { }  // ❌ Public parameterless constructor
    
    public string Name { get; set; }  // ❌ Primitive types, public setter
    public uint Bounty { get; set; }  // ❌ No encapsulation
    public bool IsKing { get; set; }  // ❌ Business rule can be bypassed
    public List<DevilFruit> EatenFruits { get; set; }  // ❌ Mutable collection exposed
}
```

## 💎 Value Objects

Value objects represent **concepts** without identity. They are **immutable** and compared by value, not reference.

### Key Principles

1. **Use `readonly record struct`** for simple value objects
2. **Validate in constructor** - throw `ArgumentException` for invalid values
3. **Provide implicit conversions** when appropriate
4. **Make them immutable** - no public setters

### ✅ Good Example

```csharp
namespace YourApp.Domain.Common.ValueObjects;

public readonly record struct Name
{
    private readonly string _value;

    public Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty.", nameof(value));

        _value = value;
    }

    public override string ToString() => _value;

    public static implicit operator string(Name name) => name._value;
    public static implicit operator Name(string value) => new(value);
}
```

### ❌ Bad Example

```csharp
// DON'T create mutable value objects
public class Name
{
    public string Value { get; set; }  // ❌ Mutable
}

// DON'T use primitive types directly in domain
public class Pirate
{
    public string Name { get; set; }  // ❌ Primitive obsession
    public uint Bounty { get; set; }  // ❌ No domain meaning
}
```

## 📜 Repository Interfaces

Repository interfaces belong in the **Domain layer** as contracts, but implementations are in the Infrastructure layer.

### Key Principles

1. **Define in the same namespace/folder as the aggregate**
2. **Inherit from `IRepository<TEntity, TId>`**
3. **Add only domain-specific query methods**
4. **Return domain types, not DTOs**

### ✅ Good Example

```csharp
using Raftel.Domain.Abstractions;

namespace YourApp.Domain.Pirates;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
    // Domain-specific queries
    Task<List<Pirate>> GetByCrewAsync(CrewId crewId, CancellationToken token);
    Task<bool> HasEatenFruitAsync(PirateId pirateId, DevilFruitId fruitId, CancellationToken token);
}
```

### ❌ Bad Example

```csharp
// DON'T put generic CRUD methods - they're already in IRepository
public interface IPirateRepository : IRepository<Pirate, PirateId>
{
    Task<Pirate> GetByIdAsync(PirateId id);  // ❌ Already in base
    Task AddAsync(Pirate pirate);  // ❌ Already in base
    Task<PirateDto> GetDtoAsync(PirateId id);  // ❌ DTOs don't belong in domain
}
```

## 🔒 Business Logic Encapsulation

Business rules should be **encapsulated** within domain entities and enforced through **methods**, not exposed through properties.

### ✅ Good Example

```csharp
public class Pirate : AggregateRoot<PirateId>
{
    private readonly DevilFruitCollection _eatenDevilFruits;

    // Business rule: Normal pirates can only eat one fruit
    public Result EatFruit(DevilFruit fruit)
    {
        if (!CanEatFruit())
        {
            return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
        }

        _eatenDevilFruits.Add(fruit);
        return Result.Success();
    }

    private bool CanEatFruit() 
        => _bodyType != BodyType.Normal || !_eatenDevilFruits.HasAny();
}
```

### ❌ Bad Example

```csharp
// DON'T expose collections or allow direct manipulation
public class Pirate : AggregateRoot<PirateId>
{
    public List<DevilFruit> EatenFruits { get; set; }  // ❌ Can bypass business rules
}

// Application layer then has to duplicate business logic
public class EatFruitHandler
{
    public async Task HandleAsync(EatFruitCommand cmd)
    {
        var pirate = await _repo.GetByIdAsync(cmd.PirateId);
        
        // ❌ Business logic leaking into application layer
        if (pirate.BodyType == BodyType.Normal && pirate.EatenFruits.Count > 0)
        {
            return Result.Failure("Cannot eat more than one fruit");
        }
        
        pirate.EatenFruits.Add(fruit);  // ❌ Direct manipulation
    }
}
```

## ⚖️ Domain Validation

Use the `Validator<T>` base class for domain validation rules.

### ✅ Good Example

```csharp
using Raftel.Domain.Validators;

namespace YourApp.Domain.Pirates;

public class PirateValidator : Validator<Pirate>
{
    public PirateValidator()
    {
        EnsureThat(p => p.Bounty > 0, PirateErrors.BountyMustBePositive);
        EnsureThat(p => !string.IsNullOrEmpty(p.Name), PirateErrors.NameRequired);
    }
}
```

## ⚠️ Domain Errors

Define domain-specific errors using the `Error` type.

### ✅ Good Example

```csharp
using Raftel.Domain.Abstractions;

namespace YourApp.Domain.Pirates;

public static class PirateErrors
{
    public static readonly Error CannotEatMoreThanOneDevilFruit = 
        new("Pirate.CannotEatMoreThanOneDevilFruit", 
            "A normal pirate cannot eat more than one Devil Fruit");
    
    public static readonly Error NameRequired = 
        new("Pirate.NameRequired", "Pirate name is required");
    
    public static readonly Error BountyMustBePositive = 
        new("Pirate.BountyMustBePositive", "Bounty must be greater than zero");
}
```

## 🧪 Testing Domain Logic

Domain logic should be **easy to test** without any infrastructure dependencies.

### ✅ Good Example

```csharp
[Fact]
public void EatFruit_WhenNormalPirateAlreadyAte_ShouldReturnFailure()
{
    // Arrange
    var pirate = Pirate.Normal("Luffy", 1_500_000);
    var gomuGomu = DevilFruit.Create("Gomu Gomu");
    pirate.EatFruit(gomuGomu);
    
    var secondFruit = DevilFruit.Create("Mera Mera");
    
    // Act
    var result = pirate.EatFruit(secondFruit);
    
    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(PirateErrors.CannotEatMoreThanOneDevilFruit);
}
```

## 📚 Naming Conventions

- **Aggregate Roots**: `[DomainConcept]` (e.g., `Pirate`, `Ship`, `Tenant`)
- **Strongly-typed IDs**: `[AggregateName]Id` (e.g., `PirateId`, `ShipId`)
- **Value Objects**: Descriptive names (e.g., `Name`, `Bounty`, `Email`)
- **Repository Interfaces**: `I[AggregateName]Repository` (e.g., `IPirateRepository`)
- **Domain Errors**: `[AggregateName]Errors` (e.g., `PirateErrors`, `TenantErrors`)
- **Specifications**: `[Condition]Specification` (e.g., `BountyOverSpecification`)

## 🎯 Do's and Don'ts

### ✅ DO

- Use strongly-typed IDs for all aggregate roots
- Encapsulate business logic in domain methods
- Use factory methods for object creation
- Make value objects immutable
- Return `Result<T>` from methods that can fail
- Keep the domain layer independent of infrastructure
- Use private setters to protect invariants
- Define repository interfaces in the domain layer

### ❌ DON'T

- Use primitive types for IDs (primitive obsession)
- Expose public setters that bypass business rules
- Put infrastructure concerns in the domain
- Reference Application, Infrastructure, or API layers
- Create mutable value objects
- Put DTOs in the domain layer
- Duplicate business logic outside the domain

## 🔗 Related Instructions

- Domain-Driven Design best practices
- Clean Architecture principles
- Object Calisthenics rules
- C# Coding Style guidelines

## 📖 Further Reading

See existing examples in:
- `src/Raftel.Domain/Features/Tenants/` - Tenant aggregate
- `demo/Raftel.Demo.Domain/Pirates/` - Pirate aggregate (comprehensive example)
- `src/Raftel.Domain/BaseTypes/` - Base classes for aggregates, entities, and IDs
