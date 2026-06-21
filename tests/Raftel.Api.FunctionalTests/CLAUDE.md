# Raftel.Api.FunctionalTests

**Functional** end-to-end tests over the real API via HTTP. Spin up the `Raftel.Api.FunctionalTests.DemoApi` host with a containerized DB and exercise AutoEndpoints-generated endpoints, authentication (OpenIddict), and error handling.

## Stack

- **xUnit 2.9.3** + **Shouldly** + **Testcontainers** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`).
- `ApiTestFactory.cs` — `WebApplicationFactory` spinning up demo host pointing to containerized DB.
- `ApiTestCollection.cs` — `[CollectionDefinition]` + `ICollectionFixture<ApiTestFactory>`: all functional test classes share **one** factory/container for the whole assembly run instead of one per class.

## Files and Coverage Areas

```
ApiTestFactory.cs           shared factory/SUT
ApiTestCollection.cs        collection definition shared by all classes below
ApiDefinitionTests.cs       correct endpoint generation (AutoEndpoints)
AuthenticationTest.cs       OpenIddict flow (/connect/token), tokens
PiratesEndpointsTests.cs    business CRUD via HTTP
ExceptionHandlingTests.cs   ProblemDetails responses (RFC 7807)
```

## Conventions

- Use `HttpClient` from `ApiTestFactory`; don't spin up the host manually.
- Decorate every test class with `[Collection(ApiTestCollection.Name)]` (not `IClassFixture<ApiTestFactory>`) so it joins the shared container instead of starting a new one.
- Test **observable HTTP contract**: status codes, JSON body, ProblemDetails format on error, authorization (401/403 without permission).
- For protected endpoints, obtain a token via password flow first.
- Data isn't reset between tests (no Respawn here, since the seeded admin role/OpenIddict client must survive); use unique values (GUIDs) for anything you create so tests don't collide.
