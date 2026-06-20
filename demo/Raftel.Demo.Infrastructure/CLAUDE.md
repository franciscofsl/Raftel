# Raftel.Demo.Infrastructure

Infrastructure of the "Pirates" example app. **Canonical reference for connecting an app to `Raftel.Infrastructure`**: defines a derived `DbContext`, EF configurations, repositories, and migrations.

## Structure

```
Data/
    TestingRaftelDbContext.cs          derives from RaftelDbContext<TestingRaftelDbContext>; declares DbSet<Pirate>, DbSet<Ship>
    TestingRaftelDbContextFactory.cs   design-time factory for `dotnet ef`
    PirateRepository.cs / ShipRepository.cs   inherit from EfRepository<,,> and implement Domain interface
    Configuration/                     IEntityTypeConfiguration per aggregate (Value Object conversions, HasTenantId(), private fields)
    Migrations/                        generated EF Core migrations
DependencyInjection.cs                 AddSampleInfrastructure(connectionString): calls AddRaftelData<TestingRaftelDbContext> and registers repos
```

## What It Demonstrates (use as template)

- **DbContext**: inherits from `RaftelDbContext<T>`, exposes `DbSet<>` and in `OnModelCreating` calls `ApplyConfigurationsFromAssembly(...)` **before** `base.OnModelCreating(...)` (which applies global filters).
- **Value Object configuration**: `HasConversion` for Ids and VOs (`PirateId`↔`Guid`, `Bounty`↔`uint`), `UsePropertyAccessMode(Field)` for private fields, `builder.HasTenantId()` for multitenancy.
- **Repositories**: nearly empty class inheriting `EfRepository<TestingRaftelDbContext, Pirate, PirateId>` and implementing `IPirateRepository`.
- **Registration**: `AddSampleInfrastructure` invokes `AddRaftelData<T>` and registers each `IXxxRepository`.

## Conventions

- Migrations generated with `dotnet ef migrations add <Name> --project demo/Raftel.Demo.Infrastructure` (uses design-time factory). Don't edit by hand.
- No business logic: only mapping, persistence, and DI. Consistent with `src/Raftel.Infrastructure/CLAUDE.md`.
