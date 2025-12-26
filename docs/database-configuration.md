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
