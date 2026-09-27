# Raftel.Api.FunctionalTests.DemoApi

Executable web host of the "Pirates" example app. It's the `Program.cs` that **wires the three layers** and registers endpoints with AutoEndpoints. Serves as **startup reference** and as SUT for functional tests (`tests/Raftel.Api.FunctionalTests`).

## Structure

```
Program.cs        root composition: AddRaftelApplication + AddSampleInfrastructure + AddEndpointGroup(...)
SeedData.cs       bootstrap data for test environment
GlobalUsings.cs   global usings
WeatherForecast.cs / Controllers/WeatherForecastController.cs   ⚠️ ASP.NET template boilerplate — removable, adds no value
```

## What It Demonstrates (use as template)

- **Application registration**: `AddRaftelApplication(cfg => { RegisterServicesFromAssembly(...); AddGlobalMiddleware(typeof(LoggingMiddleware<,>)); AddGlobalMiddleware(typeof(ValidationMiddleware<,>)); AddCommandMiddleware(typeof(TransactionMiddleware<>)); AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>)); })`. `LoggingMiddleware<,>` must be registered **first** so it wraps every other global middleware. `TransactionMiddleware<>`/`TransactionMiddleware<,>` must be registered **before** `UnitOfWorkMiddleware<>`/`UnitOfWorkMiddleware<,>` so the explicit transaction wraps the commit (and, through it, auditing and domain event dispatch).
- **Infrastructure registration**: `AddSampleInfrastructure(connectionString)`.
- **Endpoints**: `app.AddEndpointGroup(group => { group.AddQuery<...>(...); group.AddCommand<...>(...); })` — no controllers.
- HTTP pipeline order: `UseCorrelationId()` → `UseRaftelExceptionHandling()` → `UseHttpsRedirection()` → `UseRouting()` → `UseTenantMiddleware()` → `UseAuthentication()` → `UseAuthorization()` → endpoints. `UseCorrelationId()` must come before `UseRaftelExceptionHandling()` so the correlation id is already in the logging scope when an exception gets logged.

## Conventions

- Keep `Program.cs` as pure composition: registration and routing, **no business logic**.
- `WeatherForecast*` is default scaffolding residue; if you touch this project, consider removing it to avoid confusing someone exploring the example.
