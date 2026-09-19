# Raftel.Api.Server

API layer. Its central piece is **AutoEndpoints**: maps Commands/Queries to minimal API via reflection, eliminating the need for controllers. Depends only on `Infrastructure`. **EF Core forbidden here** and no business logic: only routing and orchestration.

## Structure

```
AutoEndpoints/
    EndpointRouteBuilderExtensions / AutoEndpointGroupExtensions   fluent API: AddEndpointGroup, AddCommand<T>, AddQuery<T,R>
    CommandEndpointMapper / QueryEndpointMapper                    mount endpoint and dispatch to pipeline
    CommandDefinition / QueryDefinition / RouteOptions / RouteParameters   metadata per route
    ApiParametersBuilder / OpenApiType                             parameter and OpenAPI type inference
Features/<Feature>/   <Feature>DependencyInjection.cs (feature endpoint registration) and exceptional controllers
    Users/AuthorizationController.cs   classic controller for OpenIddict flow (/connect/token)
Middlewares/          ExceptionHandlingMiddleware (+ extensions) → errors in ProblemDetails (RFC 7807)
                       CorrelationIdMiddleware (+ extensions) → resolves/echoes X-Correlation-Id
Health/               HealthEndpointExtensions (MapRaftelHealthChecks) + HealthResponseWriter (JSON, status-only vs detailed)
```

## Patterns and Practices

- **AutoEndpoints**: register routes declaratively.
  ```csharp
  app.AddEndpointGroup(group =>
  {
      group.Name = "Pirates";
      group.BaseUri = "/api/pirates";
      group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);
      group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
  });
  ```
  - Route/query parameters inferred via reflection on the Command/Query record **constructor**.
  - If the type carries `[RequiresPermission]`, authorization is applied automatically.
- **Controllers**: only as exception when AutoEndpoints doesn't fit (e.g., `AuthorizationController` for OpenIddict handshake). Always use Commands/Queries for business CRUD.
- **Errors**: `ExceptionHandlingMiddleware` centralizes everything in `ProblemDetails`, no stack traces leaked. It no longer logs directly — it enriches the current request's `IRequestEvent` with the exception and a type-derived `level` (`Debug` for `ValidationException`, `Warning` for `UnauthorizedException`, `Error` otherwise) before producing the response.
- **Correlation & wide events**: `CorrelationIdMiddleware` must be registered **before** `ExceptionHandlingMiddleware` (`app.UseCorrelationId(); app.UseRaftelExceptionHandling();`), since it wraps everything else. It resolves/echoes `X-Correlation-Id`, sets `request_id`/`method`/`path`/`status_code`/`duration_ms` on `IRequestEvent`, and is the **single point that emits the request's wide event** — exactly once per request, at whatever `level` was recorded (defaulting to `Information`). This is what guarantees "exactly one log entry per request" holds even for requests that never reach command/query dispatch (malformed JSON body, a 404 with no matching route, an ASP.NET-level 401/403). Never add a competing `ILogger` call in another middleware — enrich `IRequestEvent` instead (see `Raftel.Application/CLAUDE.md`'s "Wide Events" section).
- **Health checks**: `app.MapRaftelHealthChecks()` maps `/health/live` (no checks, anonymous), `/health/ready` (checks tagged `ready`, anonymous), and `/health` (every check, authorized by default per `HealthOptions`) — see `docs/health-checks.md`. This layer only maps endpoints and writes the JSON response (`HealthResponseWriter`); the checks themselves live in `Raftel.Infrastructure/Health` since they need EF Core, which this layer must not reference. `HealthOptions` and the `ready` tag live in `Raftel.Application.Abstractions.Health` specifically so this mapping code can read them without an `Infrastructure` reference.

## Conventions

- This layer doesn't reference `Domain` directly in practice except Response types already living in Application.
- One `<Feature>DependencyInjection.cs` file per feature to group its endpoint/service registration.
- File-scoped namespaces `Raftel.Api.Server.*`, one type per file.
