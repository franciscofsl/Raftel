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
- **Errors**: `ExceptionHandlingMiddleware` centralizes everything in `ProblemDetails`, no stack traces leaked. ⚠️ Currently swallows the real exception without logging — this is the point to enhance if you add observability.

## Conventions

- This layer doesn't reference `Domain` directly in practice except Response types already living in Application.
- One `<Feature>DependencyInjection.cs` file per feature to group its endpoint/service registration.
- File-scoped namespaces `Raftel.Api.Server.*`, one type per file.
