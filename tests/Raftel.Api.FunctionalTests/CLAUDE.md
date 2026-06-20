# Raftel.Api.FunctionalTests

**Functional** end-to-end tests over the real API via HTTP. Spin up the `Raftel.Api.FunctionalTests.DemoApi` host with a containerized DB and exercise AutoEndpoints-generated endpoints, authentication (OpenIddict), and error handling.

## Stack

- **xUnit 2.9.3** + **Shouldly** + **Testcontainers** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`).
- `ApiTestFactory.cs` — `WebApplicationFactory` spinning up demo host pointing to containerized DB.

## Files and Coverage Areas

```
ApiTestFactory.cs           shared factory/SUT
ApiDefinitionTests.cs       correct endpoint generation (AutoEndpoints)
AuthenticationTest.cs       OpenIddict flow (/connect/token), tokens
PiratesEndpointsTests.cs    business CRUD via HTTP
ExceptionHandlingTests.cs   ProblemDetails responses (RFC 7807)
```

## Conventions

- Use `HttpClient` from `ApiTestFactory`; don't spin up the host manually.
- Test **observable HTTP contract**: status codes, JSON body, ProblemDetails format on error, authorization (401/403 without permission).
- For protected endpoints, obtain a token via password flow first.
- Isolation per test; seed with demo `SeedData` when needed.
