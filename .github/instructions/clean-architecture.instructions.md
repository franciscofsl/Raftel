---
applyTo: "**/*.cs"
---

# Clean Architecture

When implementing backend services, follow these Clean Architecture principles to ensure maintainability, scalability, and separation of concerns. This rule is tailored for .NET solutions with a multi-project structure.

## 1. Solution Structure

- The solution **must** be organized into four main projects (one per layer):
  - `[project].Domain` (core business logic, entities, value objects, domain events)
  - `[project].Application` (use cases, commands, queries, interfaces for external services)
  - `[project].Infrastructure` (implementations for external services, database access, third-party integrations)
  - `[project].Api` (API endpoints, minimal API, request/response models)
- Each project must contain a marker/reference file (e.g., `DomainReference.cs`) for test discovery and architecture validation.
- Tests must be in separate projects:
  - `tests/[project].UnitTests` (for Domain and Application)
  - `tests/[project].IntegrationTests` (for Infrastructure, Api, and architecture validation)

## 2. Dependencies Between Layers

- **Domain**: has no dependencies.
- **Application**: depends only on **Domain**.
- **Infrastructure**: depends on **Application** and **Domain**.
- **Api**: depends only on **Infrastructure**.
- These dependencies **must** be enforced by automated architecture tests (e.g., NetArchTest in `ArchitectureTests.cs`).
- Forbidden dependencies (e.g., EntityFrameworkCore in Api/Domain) must be checked by tests.

## 3. Folder and File Structure

- Use a **feature-oriented** (domain-driven) folder structure in each layer (e.g., `Order/`, `Customer/`).
- Do **not** use technical root folders (Entities, ValueObjects, Services, etc.).
- Example minimal structure:

```
src/
  [project].Domain/
    DomainReference.cs
    Order/
      Order.cs
      OrderCreatedEvent.cs
    Customer/
      Customer.cs
  [project].Application/
    ApplicationReference.cs
    Order/
      ...
    Customer/
      ...
  [project].Infrastructure/
    InfrastructureReference.cs
    Order/
      ...
    Customer/
      ...
  [project].Api/
    Program.cs
    ...
tests/
  [project].UnitTests/
    ...
  [project].IntegrationTests/
    ArchitectureTests.cs
    ...
```

## 4. Coding Style and Conventions

- Use file-scoped namespaces.
- One type per file.
- Follow Microsoft .NET C# coding conventions.
- Organize files by feature/domain.

## 5. Implementation Guidelines

- **Domain Layer**: All business logic, entities, value objects, and domain events. No dependencies on other layers.
- **Application Layer**: Use cases, commands, queries, interfaces for repositories/services. No business logic.
- **Infrastructure Layer**: Implementations for interfaces, database access, external integrations. No business logic.
- **Api Layer**: Minimal API endpoints, request/response mapping. No business logic.
- Use dependency injection for all cross-layer dependencies.
- Avoid circular dependencies.
- Do not use a mediator library; call service methods directly from the Api layer.

## 6. Testing and Architecture Validation

- **Unit Tests**: In `tests/[project].UnitTests/`, for Domain and Application layers only. Use xUnit v3 and FakeItEasy for mocks.
- **Integration Tests**: In `tests/[project].IntegrationTests/`, for Infrastructure and Api layers. Use Testcontainers/Microcks for advanced scenarios.
- **Architecture Tests**: Must be present in `ArchitectureTests.cs` and:
  - Enforce allowed/forbidden dependencies between layers
  - Check for forbidden dependencies (e.g., EF Core in Api/Domain)
  - Optionally, check for immutability in Domain
- Always write tests before implementation (TDD).

## 7. Architecture Testing Example

To enforce and validate architecture rules, add automated tests in `tests/[project].IntegrationTests/ArchitectureTests.cs` using [NetArchTest](https://github.com/BenMorris/NetArchTest). Example:

```csharp
using System.Reflection;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

using Order.Application;
using Order.Domain;
using Order.Infrastructure;

namespace Order.IntegrationTests;

public class ArchitectureTests
{
    private static string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string ApiNamespace = "Api";
    private const string ApplicationNamespace = "Application";
    private const string DomainNamespace = "Domain";
    private const string InfrastructureNamespace = "Infrastructure";

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationReference).Assembly;
    private static readonly Assembly DomainAssembly = typeof(DomainReference).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InfrastructureReference).Assembly;

    public ITestOutputHelper TestOutputHelper { get; }

    public ArchitectureTests(ITestOutputHelper testOutputHelper)
    {
        this.TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Api_ShouldOnlyDependOn_Application()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace(ApiNamespace)
            .Should().HaveDependencyOn(ApplicationNamespace)
            .And()
            .NotHaveDependencyOn(DomainNamespace)
            .And()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"{ApiNamespace} should only depend on {ApplicationNamespace}");
    }

    // ...other architecture tests for Application, Infrastructure, Domain, and forbidden dependencies...
}
```

- Adapt namespaces, assemblies, and rules to your solution.
- Add tests to check for forbidden dependencies (e.g., EntityFrameworkCore in Api/Domain) and for immutability in Domain types if relevant.
- Run these tests with `dotnet test` to ensure architecture rules are enforced after every change.

## Additional Guidelines

1. Use dependency injection to manage dependencies across layers.
2. Avoid circular dependencies between layers.
3. Write unit tests for **Domain** and **Application** layers.
4. Use integration tests for **Infrastructure** and **Api** layers.
5. Follow SOLID principles within each layer.
6. Avoid using a mediator library; instead, directly call service methods from the **Api** layer.

---

# Complete Implementation Examples Using Raftel

Below are complete, working examples from the Raftel framework demonstrating Clean Architecture principles. These examples show the full implementation flow across all layers.

## Example 1: Domain Layer - Entities and Value Objects

### Entity: Pirate (Aggregate Root)

```csharp
// File: Raftel.Demo.Domain/Pirates/Pirate.cs
using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private readonly DevilFruitCollection _eatenDevilFruits;
    private readonly BodyType _bodyType;

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

    public Name Name { get; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; private set; }

    // Factory Methods
    public static Pirate Normal(Name name, Bounty bounty) => new(name, bounty, BodyType.Normal);
    public static Pirate Special(Name name, Bounty bounty) => new(name, bounty, BodyType.Special);

    // Business Logic
    public void FoundOnePiece() => IsKing = true;

    public Result EatFruit(DevilFruit fruit)
    {
        if (!CanEatFruit())
        {
            return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
        }

        _eatenDevilFruits.Add(fruit);
        return Result.Success();
    }

    private bool CanEatFruit() => _bodyType != BodyType.Normal || !_eatenDevilFruits.HasAny();
}
```

### Value Objects

```csharp
// File: Raftel.Demo.Domain/Pirates/ValueObjects/PirateId.cs
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates.ValueObjects;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(NewGuid());
}
```

```csharp
// File: Raftel.Demo.Domain/Pirates/ValueObjects/Bounty.cs
namespace Raftel.Demo.Domain.Pirates.ValueObjects;

public readonly record struct Bounty
{
    private readonly uint _value;

    public Bounty(uint value)
    {
        if (value < 0)
            throw new ArgumentException("Bounty cannot be negative.", nameof(value));

        _value = value;
    }

    public override string ToString() => $"{_value:N0} berries";

    public static implicit operator uint(Bounty bounty) => bounty._value;
    public static implicit operator Bounty(uint value) => new(value);
}
```

### Domain Errors

```csharp
// File: Raftel.Demo.Domain/Pirates/PirateErrors.cs
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing =>
        new("Pirate.Name", "Luffy should be The Pirate King.");

    public static Error CannotEatMoreThanOneDevilFruit =>
        new("Pirate.EatenDevilFruits", "Pirate cannot eat more than one Devil Fruit.");
}
```

### Repository Interface (in Domain)

```csharp
// File: Raftel.Demo.Domain/Pirates/IPirateRepository.cs
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}
```

**Key Points - Domain Layer:**

- Entities contain business logic and domain rules
- Value objects are immutable and contain validation
- Factory methods for creating entities
- Domain errors are strongly typed
- Repository interfaces defined in domain (implementation in Infrastructure)
- No dependencies on other layers

---

## Example 2: Application Layer - Commands and Queries

### Command: Create Pirate

```csharp
// File: Raftel.Demo.Application/Pirates/CreatePirate/CreatePirateCommand.cs
using Raftel.Application.Authorization;
using Raftel.Application.Commands;
using Raftel.Demo.Application.Pirates;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

[RequiresPermission(PiratesPermissions.Management)]
public record CreatePirateCommand(string Name, uint Bounty, bool IsKing = false) : ICommand;
```

```csharp
// File: Raftel.Demo.Application/Pirates/CreatePirate/CreatePirateCommandHandler.cs
using Raftel.Application.Commands;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public sealed class CreatePirateCommandHandler(IPirateRepository repository)
    : ICommandHandler<CreatePirateCommand>
{
    public async Task<Result> HandleAsync(CreatePirateCommand request, CancellationToken token = default)
    {
        await repository.AddAsync(Pirate.Normal(request.Name, request.Bounty), token);
        return Result.Success();
    }
}
```

### Validator for Command

```csharp
// File: Raftel.Demo.Application/Pirates/CreatePirate/CreatePirateCommandValidator.cs
using Raftel.Domain.Validators;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public class CreatePirateCommandValidator : Validator<CreatePirateCommand>
{
    public CreatePirateCommandValidator()
    {
        EnsureThat(cmd => !string.IsNullOrWhiteSpace(cmd.Name), CreatePirateErrors.NameRequired);
        EnsureThat(cmd => !cmd.IsKing || cmd.Name == "Luffy", CreatePirateErrors.KingMustBeLuffy);
    }
}
```

### Query: Get Pirate By Id

```csharp
// File: Raftel.Demo.Application/Pirates/GetPirateById/GetPirateByIdQuery.cs
using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.GetPirateById;

public sealed record GetPirateByIdQuery(Guid Id, string Name, int? MaxBounty)
    : IQuery<GetPirateByIdResponse>;
```

```csharp
// File: Raftel.Demo.Application/Pirates/GetPirateById/GetPirateByIdQueryHandler.cs
using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.GetPirateById;

internal sealed class GetPirateByIdQueryHandler
    : IQueryHandler<GetPirateByIdQuery, GetPirateByIdResponse>
{
    public async Task<Result<GetPirateByIdResponse>> HandleAsync(
        GetPirateByIdQuery request,
        CancellationToken token = default)
    {
        var mugiwara = MugiwaraCrew.All.FirstOrDefault(_ => _.Id == request.Id);

        if (mugiwara is null)
        {
            return Result.Failure<GetPirateByIdResponse>(
                new Error("PirateNotFound", "Pirate not found"));
        }

        return Result<GetPirateByIdResponse>.Success(new GetPirateByIdResponse
        {
            Bounty = mugiwara.Bounty,
            Id = mugiwara.Id,
            Name = mugiwara.Name
        });
    }
}
```

**Key Points - Application Layer:**

- Commands for write operations, Queries for read operations
- Handlers are responsible for orchestration, not business logic
- Validators separate from handlers for reusability
- Use Result pattern for error handling
- Authorization attributes for permissions
- Only depends on Domain layer

---

## Example 3: Infrastructure Layer - Repositories and Data Access

### Repository Implementation

```csharp
// File: Raftel.Demo.Infrastructure/Data/PirateRepository.cs
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}
```

### DbContext Configuration

```csharp
// File: Raftel.Demo.Infrastructure/Data/TestingRaftelDbContext.cs
using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Demo.Infrastructure.Data;

public class TestingRaftelDbContext : RaftelDbContext<TestingRaftelDbContext>
{
    public DbSet<Pirate> Pirates { get; set; }
    public DbSet<Ship> Ships { get; set; }

    public TestingRaftelDbContext(DbContextOptions<TestingRaftelDbContext> options)
        : base(options)
    {
    }

    public TestingRaftelDbContext(
        DbContextOptions<TestingRaftelDbContext> options,
        IDataFilter dataFilter)
        : base(options, dataFilter)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestingRaftelDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Entity Configuration

```csharp
// File: Raftel.Demo.Infrastructure/Data/Configuration/PirateConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;

namespace Raftel.Demo.Infrastructure.Data.Configuration;

public class PirateConfiguration : IEntityTypeConfiguration<Pirate>
{
    public void Configure(EntityTypeBuilder<Pirate> builder)
    {
        builder.HasKey(p => p.Id);

        // Configure PirateId (Value Object)
        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new PirateId(value)
            );

        // Configure Name (Value Object)
        builder.Property(p => p.Name)
            .HasConversion(
                name => (string)name,
                value => (Name)value
            )
            .HasColumnName("Name")
            .IsRequired();

        // Configure Bounty (Value Object)
        builder.Property(p => p.Bounty)
            .HasConversion(
                bounty => (uint)bounty,
                value => (Bounty)value
            )
            .HasColumnName("Bounty")
            .IsRequired();

        // Configure private field
        builder.Property(typeof(BodyType), "_bodyType")
            .HasField("_bodyType")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("BodyType")
            .IsRequired();

        // Configure multi-tenancy
        builder.HasTenantId();
    }
}
```

### Dependency Injection

```csharp
// File: Raftel.Demo.Infrastructure/DependencyInjection.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure;

namespace Raftel.Demo.Infrastructure;

public static class DependencyInjection
{
    public static void AddSampleInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = connectionString
            })
            .Build();

        services.AddRaftelData<TestingRaftelDbContext>(configuration, "TestConnection");
        services.AddScoped<IPirateRepository, PirateRepository>();
        services.AddScoped<IShipRepository, ShipRepository>();
    }
}
```

**Key Points - Infrastructure Layer:**

- Implements interfaces defined in Domain/Application
- Entity Framework Core configuration for persistence
- Value object conversions in EF configuration
- Dependency injection registration
- No business logic here

---

## Example 4: API Layer - Endpoints

### Program.cs - Application Setup

```csharp
// File: Raftel.Api.FunctionalTests.DemoApi/Program.cs
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Application Layer
builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
    cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
});

// Register Infrastructure Layer
builder.Services.AddSampleInfrastructure(
    builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Auto-generate endpoints from Commands and Queries
app.AddEndpointGroup(group =>
{
    group.Name = "Pirates";
    group.BaseUri = "/api/pirates";
    group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);
    group.AddQuery<GetPirateByFilterQuery, GetPirateByFilterResponse>("", HttpMethod.Get);
    group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
});

app.Run();
```

**Key Points - API Layer:**

- Minimal setup with dependency injection
- Auto-endpoint generation from Commands/Queries
- Middleware pipeline configuration
- No business logic - just routing and orchestration
- Only depends on Infrastructure (which includes Application and Domain)

---

## Example 5: Testing

### Unit Tests - Domain Layer

```csharp
// Example: Testing Domain Entity (using xUnit v3 and Shouldly)
public class PirateTests
{
    [Fact]
    public void Pirate_Should_BecomeKing_When_FoundOnePiece()
    {
        // Arrange
        var pirate = Pirate.Normal("Luffy", 3000000000);

        // Act
        pirate.FoundOnePiece();

        // Assert
        pirate.IsKing.ShouldBeTrue();
    }

    [Fact]
    public void NormalPirate_Should_NotEat_MoreThanOneDevilFruit()
    {
        // Arrange
        var pirate = Pirate.Normal("Blackbeard", 2247600000);
        var fruit1 = DevilFruit.Create("Gomu Gomu");
        var fruit2 = DevilFruit.Create("Mera Mera");

        // Act
        pirate.EatFruit(fruit1);
        var result = pirate.EatFruit(fruit2);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PirateErrors.CannotEatMoreThanOneDevilFruit);
    }
}
```

### Integration Tests - Application Layer

```csharp
// File: Raftel.Application.UnitTests/DependencyInjectionTests.cs
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Demo.Application.Pirates.CreatePirate;

public class DependencyInjectionTests
{
    [Fact]
    public void AddRaftelApplication_ShouldRegisterCommandHandler_FromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IPirateRepository>(_ => Substitute.For<IPirateRepository>());

        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
        });

        var provider = services.BuildServiceProvider();

        // Act
        var handler = provider.GetService<ICommandHandler<CreatePirateCommand>>();

        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<CreatePirateCommandHandler>();
    }
}
```

**Key Points - Testing:**

- Unit tests for Domain and Application layers
- Use mocking frameworks (FakeItEasy, NSubstitute) for dependencies
- Integration tests for Infrastructure
- Test dependency injection configuration
- Use xUnit v3 for test framework

---

## Summary: Layer Communication Flow

```
User Request → API Endpoint → Command/Query Handler → Domain Entity → Repository → Database
                  ↓                    ↓                    ↓              ↓
            (API Layer)      (Application Layer)   (Domain Layer)  (Infrastructure)
```

**Example Flow: Creating a Pirate**

1. **API Layer**: `POST /api/pirates` receives JSON `{ "name": "Luffy", "bounty": 3000000000 }`
2. **Auto-endpoint** maps to `CreatePirateCommand`
3. **Application Layer**: `CreatePirateCommandHandler` receives command
4. **Validation Middleware**: `CreatePirateCommandValidator` validates the command
5. **Handler**: Creates domain entity using `Pirate.Normal(name, bounty)`
6. **Domain Layer**: `Pirate` entity created with business rules applied
7. **Repository**: `IPirateRepository.AddAsync()` called
8. **Infrastructure Layer**: `PirateRepository` persists to database via EF Core
9. **Unit of Work Middleware**: Commits transaction
10. **API Layer**: Returns `200 OK` or appropriate status

This structure ensures clear separation of concerns, testability, and maintainability.

# References

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/TheCleanArchitecture.html)
- [Raftel Framework Repository](https://github.com/franciscofsl/Raftel)
