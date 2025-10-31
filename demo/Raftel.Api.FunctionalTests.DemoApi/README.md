# Raftel Demo API

This is a demo application showcasing the Raftel framework capabilities.

## Database Configuration

The demo API supports both SQL Server and PostgreSQL as database providers.

### Using SQL Server (Default)

The default configuration uses SQL Server. No changes needed to `appsettings.json`:

```json
{
  "Database": {
    "Provider": "SqlServer"
  }
}
```

Connection string in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=RaftelTests;User Id=sa;Password=SqlServer_Docker2024;TrustServerCertificate=True;"
  }
}
```

### Using PostgreSQL

To use PostgreSQL instead:

1. Update `appsettings.json` or create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=RaftelTests;Username=postgres;Password=YourPassword;"
  },
  "Database": {
    "Provider": "PostgreSql"
  }
}
```

2. Ensure PostgreSQL is running (e.g., via Docker):

```bash
docker run --name postgres-raftel -e POSTGRES_PASSWORD=YourPassword -p 5432:5432 -d postgres:latest
```

See `appsettings.PostgreSQL.example.json` for a complete configuration example.

## Running the Demo

```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port configured in launchSettings.json).
