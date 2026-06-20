# Raftel

A **.NET 10** micro-framework for building APIs following **Clean Architecture + DDD + CQRS**, with custom implementations (no MediatR/AutoMapper) of command/query pipeline, permission-based authorization, soft-delete, audit logging, multitenancy, OAuth2/OIDC authentication, and automatic minimal-API endpoint generation.

> This `CLAUDE.md` is the hierarchy index. Each `csproj` has its own `CLAUDE.md` documenting structure and practices for that layer. **Always read the `CLAUDE.md` closest to the code you're editing** before making changes.

## Project Map

```
src/
  Raftel.Domain          → Tactical DDD: Entity/AggregateRoot, Value Objects, Result, Specifications, Validators
  Raftel.Application     → CQRS: Commands/Queries, Handlers, middleware pipeline (custom mediator)
  Raftel.Infrastructure  → EF Core, repositories, multitenancy, auth (Identity + OpenIddict), interceptors
  Raftel.Api.Server      → AutoEndpoints (minimal API), error middleware (ProblemDetails)
  Raftel.Api.Client      → HTTP client helpers (QueryFilter)
  Raftel.Shared          → Cross-cutting utilities with no domain dependencies
tools/
  Raftel.Cli             → Scaffolding CLI (generates Commands/Queries with Roslyn)
demo/                    → Example app "Pirates" (One Piece) documenting the framework
tests/                   → unit / integration / functional (xUnit, Shouldly, NSubstitute, Testcontainers)
```

## Layer Dependency Rule (Non-negotiable)

- **Domain**: no dependencies.
- **Application**: depends only on **Domain**.
- **Infrastructure**: depends on **Application** and **Domain**.
- **Api**: depends only on **Infrastructure** (which brings Application and Domain).
- `Microsoft.EntityFrameworkCore` is **forbidden** in `Domain` and the `Api` layer.

Request flow:
`HTTP → AutoEndpoint → Command/Query Handler → Aggregate → Repository → DB`

## Global Conventions

- **Vertical feature organization** (`Features/Users/CreateUser/...`), never by technical folders (`Services/`, `DTOs/`, `Entities/`).
- File-scoped namespaces, one type per file, 4-space indentation, `sealed` classes by default.
- **Result/Result<T>** pattern for errors; no exceptions for business-logic flow.
- **Object Calisthenics** and **DDD** govern domain and application code (see `.github/instructions/`).
- Domain method names oriented to business intent (`AssignRole`, `Rename`), never generic CRUD.
- TDD: test before implementation. Commits follow Conventional Commits.

## Common Commands

```bash
dotnet build Raftel.sln
dotnet test                                   # entire suite
dotnet test tests/Raftel.Domain.Tests         # specific project
```

## Additional Sources of Truth

Detailed guides live in `.github/instructions/`:
`clean-architecture`, `domain-driven-design`, `object-calisthenics`, `coding-style-csharp`, `conventional-commits`, `unit-and-integration-tests-csharp`.
