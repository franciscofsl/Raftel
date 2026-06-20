# Raftel.Application.UnitTests

**Unit** tests of the application layer: handlers, validators, middleware pipeline, and DI registration. Dependencies (repositories, services) replaced by test doubles; no real database.

## Stack

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute** (`Substitute.For<IPirateRepository>()`).
- `GlobalUsings.cs` centralizes common test usings.
- Access to `Raftel.Application` internals via `InternalsVisibleTo`.

## What Gets Tested Here

- **Handlers** in isolation: mock the repository, verify orchestration and returned `Result`.
- **Validators**: that `EnsureThat(...)` fails/passes per input.
- **DI/pipeline**: `DependencyInjectionTests` verify `AddRaftelApplication` registers handlers, validators, and middlewares via reflection.

## Conventions

- Arrange/Act/Assert; behavior-oriented names.
- Verify NSubstitute interactions (`Received()`) only when the effect is the call itself (e.g., `repository.AddAsync`).
- No EF Core or containers: if you need real DB, that's `Raftel.Application.IntegrationTests`.
