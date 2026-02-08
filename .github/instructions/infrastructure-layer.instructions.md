---
applyTo: "**/*Infrastructure*/**/*.cs"
---

# Infrastructure Layer Coding Instructions

## Overview

The Infrastructure layer provides **concrete implementations** for interfaces defined in the Domain and Application layers. It handles persistence, external services, and technical concerns.

## 🎯 What Belongs in the Infrastructure Layer

✅ **DO include:**
- Repository implementations
- Database context (EF Core DbContext)
- Entity configurations (IEntityTypeConfiguration)
- Database migrations
- External service integrations
- Caching implementations
- File system access
- Email/SMS services
- Message queue implementations
- Authentication/Authorization infrastructure
- Dependency injection registration

❌ **DO NOT include:**
- Business rules (those belong in Domain)
- Use case orchestration (belongs in Application)
- HTTP/API controllers or endpoints (belongs in API layer)
- Domain models directly - use EF configurations instead

## 📁 File and Folder Structure

```
Raftel.Infrastructure/
├── Data/
│   ├── [AppName]DbContext.cs
│   ├── EfRepository.cs (base class)
│   ├── Configuration/
│   │   └── [Entity]Configuration.cs
│   ├── Migrations/
│   │   └── [Timestamp]_[Description].cs
│   └── Repositories/
│       └── [Entity]Repository.cs
├── Authentication/
│   └── [AuthService].cs
├── Multitenancy/
│   └── [TenantService].cs
└── DependencyInjection.cs
```

## 🗄️ Repository Implementations

Repository implementations provide data access for domain aggregates.

### Key Principles

1. **Inherit from `EfRepository<TDbContext, TEntity, TId>`**
2. **Implement domain repository interface**
3. **Keep repositories simple** - they translate domain operations to EF Core
4. **Only add methods that are in the interface**
5. **Use the DbContext passed to the base constructor**

### ✅ Good Example

```csharp
using Raftel.Infrastructure.Data;

namespace YourApp.Infrastructure.Data;

public class PirateRepository(YourAppDbContext dbContext)
    : EfRepository<YourAppDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
    // Only implement domain-specific methods from the interface
    public async Task<List<Pirate>> GetByCrewAsync(CrewId crewId, CancellationToken token)
    {
        return await dbContext.Set<Pirate>()
            .Where(p => p.CrewId == crewId)
            .ToListAsync(token);
    }
    
    public async Task<bool> HasEatenFruitAsync(
        PirateId pirateId, 
        DevilFruitId fruitId, 
        CancellationToken token)
    {
        return await dbContext.Set<Pirate>()
            .AnyAsync(p => p.Id == pirateId && p.EatenFruits.Any(f => f.Id == fruitId), token);
    }
}
```

### ❌ Bad Example

```csharp
// DON'T bypass the base repository
public class PirateRepository : IPirateRepository
{
    private readonly DbContext _dbContext;
    
    // ❌ Reimplementing base functionality
    public async Task<Pirate> GetByIdAsync(PirateId id, CancellationToken token)
    {
        return await _dbContext.Set<Pirate>().FindAsync(id);
    }
    
    // ❌ Adding methods not in the interface
    public async Task<List<Pirate>> GetAllAsync()
    {
        return await _dbContext.Set<Pirate>().ToListAsync();
    }
}
```

## 🏗️ Entity Configurations

Entity configurations map domain aggregates to database tables using EF Core's Fluent API.

### Key Principles

1. **Implement `IEntityTypeConfiguration<TEntity>`**
2. **Configure all properties explicitly** - avoid conventions when possible
3. **Use value conversions** for value objects and strongly-typed IDs
4. **Configure owned entities** for collections
5. **Apply multi-tenancy** using `HasTenantId()`
6. **Apply auditing** using `HasUserAuditing()`
7. **Use backing fields** for encapsulated properties

### ✅ Good Example

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Infrastructure.Data;

namespace YourApp.Infrastructure.Data.Configuration;

public class PirateConfiguration : IEntityTypeConfiguration<Pirate>
{
    public void Configure(EntityTypeBuilder<Pirate> builder)
    {
        // Primary key
        builder.HasKey(p => p.Id);

        // Strongly-typed ID conversion
        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new PirateId(value)
            );

        // Value object conversions
        builder.Property(p => p.Name)
            .HasConversion(
                name => (string)name,
                value => (Name)value
            )
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Bounty)
            .HasConversion(
                bounty => (uint)bounty,
                value => (Bounty)value
            )
            .HasColumnName("Bounty")
            .IsRequired();
        
        // Backing field for encapsulated properties
        builder.Property(typeof(BodyType), "_bodyType")
            .HasField("_bodyType")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("BodyType")
            .IsRequired();
        
        // Many-to-many relationship
        builder
            .HasMany<DevilFruit>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EatenDevilFruitsByPirates",
                right => right.HasOne<DevilFruit>()
                    .WithMany()
                    .HasForeignKey("DevilFruitId")
                    .IsRequired(),
                left => left.HasOne<Pirate>()
                    .WithMany()
                    .HasForeignKey("PirateId")
                    .IsRequired()
            );

        // Configure multi-tenancy
        builder.HasTenantId();

        // Configure user auditing
        builder.HasUserAuditing();
    }
}
```

### ❌ Bad Example

```csharp
// DON'T rely on conventions or leave things unconfigured
public class PirateConfiguration : IEntityTypeConfiguration<Pirate>
{
    public void Configure(EntityTypeBuilder<Pirate> builder)
    {
        // ❌ Only configuring the key, relying on conventions for everything else
        builder.HasKey(p => p.Id);
        
        // ❌ Not converting strongly-typed IDs
        // ❌ Not converting value objects
        // ❌ Not applying multi-tenancy
        // ❌ Not applying auditing
    }
}
```

## 🗃️ Database Context

The DbContext is the entry point for EF Core.

### Key Principles

1. **Inherit from `RaftelDbContext<TDbContext>`**
2. **Register entities using `DbSet<TEntity>`**
3. **Apply configurations** using `ApplyConfigurationsFromAssembly`
4. **Use constructor injection** for options

### ✅ Good Example

```csharp
using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data;

namespace YourApp.Infrastructure.Data;

public class YourAppDbContext(DbContextOptions<YourAppDbContext> options) 
    : RaftelDbContext<YourAppDbContext>(options)
{
    // DbSets for aggregates
    public DbSet<Pirate> Pirates => Set<Pirate>();
    public DbSet<Ship> Ships => Set<Ship>();
    public DbSet<DevilFruit> DevilFruits => Set<DevilFruit>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourAppDbContext).Assembly);
    }
}
```

### ❌ Bad Example

```csharp
// DON'T forget to call base.OnModelCreating
public class YourAppDbContext : RaftelDbContext<YourAppDbContext>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ❌ Not calling base - will break Raftel's built-in features
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourAppDbContext).Assembly);
    }
}

// DON'T manually configure entities in OnModelCreating
public class YourAppDbContext : RaftelDbContext<YourAppDbContext>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // ❌ Don't configure inline - use IEntityTypeConfiguration
        modelBuilder.Entity<Pirate>().HasKey(p => p.Id);
        modelBuilder.Entity<Pirate>().Property(p => p.Name).IsRequired();
    }
}
```

## 🔄 Database Migrations

Use EF Core migrations to manage database schema changes.

### Key Principles

1. **Create migrations** using `dotnet ef migrations add`
2. **Review migration code** before applying
3. **Name migrations descriptively** (e.g., `AddPirateCrewRelationship`)
4. **Apply migrations** in startup or through deployment pipelines

### ✅ Commands

```bash
# Create a new migration
dotnet ef migrations add AddPirateCrewRelationship --project src/YourApp.Infrastructure

# Apply migrations
dotnet ef database update --project src/YourApp.Infrastructure

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/YourApp.Infrastructure
```

## 📦 Dependency Injection

Register infrastructure services in the `DependencyInjection.cs` file.

### Key Principles

1. **Use extension methods** on `IServiceCollection`
2. **Register DbContext** with proper lifetime (scoped)
3. **Register repositories** as scoped services
4. **Configure connection strings** from configuration

### ✅ Good Example

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YourApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<YourAppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
        });
        
        // Register repositories
        services.AddScoped<IPirateRepository, PirateRepository>();
        services.AddScoped<IShipRepository, ShipRepository>();
        services.AddScoped<IDevilFruitRepository, DevilFruitRepository>();
        
        // Register other infrastructure services
        services.AddScoped<IEmailService, EmailService>();
        
        return services;
    }
}
```

### ❌ Bad Example

```csharp
// DON'T register services as singleton when they should be scoped
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(...)
    {
        // ❌ DbContext should be scoped, not singleton
        services.AddSingleton<YourAppDbContext>();
        
        // ❌ Repositories should be scoped, not transient
        services.AddTransient<IPirateRepository, PirateRepository>();
        
        return services;
    }
}
```

## 🔐 External Service Integration

Wrap external services with interfaces defined in the Application layer.

### ✅ Good Example

```csharp
// Application layer interface
namespace YourApp.Application.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken token);
}

// Infrastructure implementation
namespace YourApp.Infrastructure.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendAsync(
        string to, 
        string subject, 
        string body, 
        CancellationToken token)
    {
        var smtpClient = new SmtpClient(configuration["Email:Host"])
        {
            Port = int.Parse(configuration["Email:Port"]),
            Credentials = new NetworkCredential(
                configuration["Email:Username"],
                configuration["Email:Password"]
            )
        };
        
        var message = new MailMessage(configuration["Email:From"], to, subject, body);
        await smtpClient.SendMailAsync(message, token);
    }
}
```

## 💾 Caching Strategies

Implement caching using `IDistributedCache` or `IMemoryCache`.

### ✅ Good Example

```csharp
public class CachedPirateRepository(
    PirateRepository repository,
    IDistributedCache cache) : IPirateRepository
{
    public async Task<Pirate?> GetByIdAsync(PirateId id, CancellationToken token)
    {
        var cacheKey = $"pirate:{id}";
        var cached = await cache.GetStringAsync(cacheKey, token);
        
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<Pirate>(cached);
        }
        
        var pirate = await repository.GetByIdAsync(id, token);
        
        if (pirate is not null)
        {
            await cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(pirate),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                token);
        }
        
        return pirate;
    }
    
    // Delegate other methods to the base repository
    public Task AddAsync(Pirate entity, CancellationToken token) 
        => repository.AddAsync(entity, token);
}
```

## 🧪 Testing Infrastructure

Test repository implementations and configurations.

### ✅ Good Example

```csharp
public class PirateRepositoryTests
{
    [Fact]
    public async Task GetByCrewAsync_ShouldReturnPiratesInCrew()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new PirateRepository(context);
        
        var crewId = CrewId.New();
        var pirate1 = Pirate.Normal("Luffy", 1_500_000);
        var pirate2 = Pirate.Normal("Zoro", 1_111_000);
        
        // ... setup test data
        
        // Act
        var result = await repository.GetByCrewAsync(crewId, CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(2);
    }
    
    private YourAppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<YourAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        return new YourAppDbContext(options);
    }
}
```

## 📚 Naming Conventions

- **DbContext**: `[AppName]DbContext` (e.g., `YourAppDbContext`, `TestingRaftelDbContext`)
- **Repositories**: `[Entity]Repository` (e.g., `PirateRepository`, `ShipRepository`)
- **Entity Configurations**: `[Entity]Configuration` (e.g., `PirateConfiguration`)
- **Migrations**: `[Timestamp]_[Description]` (auto-generated by EF Core)

## 🎯 Do's and Don'ts

### ✅ DO

- Inherit from `EfRepository<TDbContext, TEntity, TId>` for repositories
- Implement `IEntityTypeConfiguration<TEntity>` for entity mappings
- Use value conversions for strongly-typed IDs and value objects
- Apply `HasTenantId()` for multi-tenant entities
- Apply `HasUserAuditing()` for audit trail
- Use backing fields for encapsulated domain properties
- Register services with appropriate lifetimes (scoped for DbContext and repositories)
- Wrap external services with application interfaces
- Call `base.OnModelCreating()` in your DbContext

### ❌ DON'T

- Bypass domain methods when manipulating entities
- Put business logic in repositories
- Reference the API layer
- Register DbContext as singleton
- Forget to apply configurations using `ApplyConfigurationsFromAssembly`
- Mix configuration code with DbContext - use `IEntityTypeConfiguration`
- Expose EF Core types in domain or application layers
- Create repositories that don't implement domain interfaces

## 🔗 Related Instructions

- Domain Layer instructions (for repository interfaces)
- Application Layer instructions (for use cases)
- Clean Architecture principles
- C# Coding Style guidelines

## 📖 Further Reading

See existing examples in:
- `src/Raftel.Infrastructure/Data/` - Base repository and DbContext
- `demo/Raftel.Demo.Infrastructure/Data/` - Example configurations and repositories
- `src/Raftel.Infrastructure/Authentication/` - Authentication services
