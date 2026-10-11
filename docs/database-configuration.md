# Database Configuration

Raftel supports multiple database providers, allowing you to choose between **SQL Server** and **PostgreSQL** based on your project requirements.

## Supported Database Providers

- **SQL Server** (default)
- **PostgreSQL**

## Configuration

The database provider is configured through the `appsettings.json` file using the `Database` section:

### SQL Server Configuration

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=RaftelDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Database": {
    "Provider": "SqlServer"
  }
}
```

### PostgreSQL Configuration

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=RaftelDb;Username=postgres;Password=YourPassword;"
  },
  "Database": {
    "Provider": "PostgreSql"
  }
}
```

## Usage in Code

When setting up your infrastructure in `Program.cs` or `Startup.cs`, the database provider will be automatically selected based on your configuration:

```csharp
builder.Services.AddRaftelData<YourDbContext>(
    builder.Configuration,
    connectionStringName: "Default");
```

The framework will automatically:
1. Read the `Database:Provider` setting from configuration
2. Configure Entity Framework Core with the appropriate database provider
3. Use the connection string from `ConnectionStrings:Default`

## Switching Between Providers

To switch between database providers:

1. Update the `Database:Provider` value in `appsettings.json`
2. Update the connection string format to match the target database
3. Generate new migrations if needed for database-specific features

## Optimistic Concurrency

Without a concurrency token, two callers who read the same aggregate and then each save an edit silently
overwrite one another (the last write wins and the first caller is never told). Raftel can detect this per
entity, opt-in.

### Declaring an entity as version-tracked

Call `HasOptimisticConcurrency()` in the entity's `IEntityTypeConfiguration`, next to `HasSoftDelete()` /
`HasTenantId()`. The framework's `User`, `Tenant` and `Role` already do.

```csharp
builder.HasOptimisticConcurrency();
```

The token is a database-managed shadow property named `Version`, added by `RaftelDbContext` once the active
provider is known, so nothing is incremented by hand and the aggregate is not touched:

| Provider   | Token                                                    | Schema impact                  |
|------------|----------------------------------------------------------|--------------------------------|
| SQL Server | `rowversion` column                                      | New column: add a migration    |
| PostgreSQL | `xmin` system column (read as an unsigned integer)       | None: `xmin` already exists    |

Entities that do not call `HasOptimisticConcurrency()` keep last-write-wins behaviour. The token is excluded
from audit logs.

### Reporting a conflict

A rejected save raises `ConcurrencyConflictException` (no EF Core type involved). Register
`ConcurrencyMiddleware<>` and `ConcurrencyMiddleware<,>` **before** `TransactionMiddleware` to turn it into
a failed result with code `Concurrency.Conflict`, answered as HTTP **409**:

```csharp
cfg.AddCommandMiddleware(typeof(ConcurrencyMiddleware<>));
cfg.AddCommandMiddleware(typeof(ConcurrencyMiddleware<,>));
cfg.AddCommandMiddleware(typeof(TransactionMiddleware<>));
cfg.AddCommandMiddleware(typeof(TransactionMiddleware<,>));
cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
```

### Catching conflicts between requests (ETag / If-Match)

The token on its own only detects a conflict inside a single request. Each HTTP request loads the aggregate
afresh, so to catch "A and B both read, then both save" the client has to send back the version it read.
This is off by default:

```csharp
cfg.ConfigureConcurrency(options => options.EnableETags = true);
```

- A query result that implements `IVersionedResult` is returned with a strong `ETag` header.
- A command request may send `If-Match: "<etag>"`. The version is compared by the database in the same
  `UPDATE`/`DELETE` that writes the change; a mismatch answers **412 Precondition Failed**. A malformed
  header answers **400**.
- Mark a command `[RequiresExpectedVersion]` to forbid blind writes: no `If-Match` (or `*`) answers
  **428 Precondition Required**.
- No header: last write wins, as before.

Treat the version as opaque; its format depends on the provider. `If-Match` is meant for commands that change
a single version-tracked aggregate.

### Migrating an existing application

On SQL Server, generate and apply a migration that adds the `Version` (`rowversion`) column to `User`,
`Tenant`, `Role` and any entity of yours that opts in. On PostgreSQL the model snapshot changes but the
generated migration adds no column.

## Database Migrations

When using migrations, ensure you have the appropriate tools installed:

### For SQL Server
```bash
dotnet ef migrations add InitialCreate
```

### For PostgreSQL
```bash
dotnet ef migrations add InitialCreate
```

Note: Some database-specific features may require separate migration files for each provider.

## Related

See [Health Checks](health-checks.md) for the `PendingMigrationsHealthCheck`, which reports
unhealthy while the schema has migrations that have not been applied.
