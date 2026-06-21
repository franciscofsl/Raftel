# Raftel.Domain

Domain layer (tactical DDD). **Framework core: no dependencies to any other layer or EF Core.** Contains reusable primitives (`Entity`, `Result`, `Specification`, `Validator`) and framework-owned domain features (`Users`, `Tenants`, `Authorization`).

## Structure

```
Abstractions/        Error, Result/Result<T>, IRepository<TEntity,TId>, IDomainEvent, IHasDomainEvents
BaseTypes/           Entity<TId>, AggregateRoot<TId>, TypedId<T>, TypedGuidId
Specifications/      Specification<T> + And/OrSpecification (Expression<Func<T,bool>>)
Validators/          Validator<T>, ValidationRule, ValidationResult (EnsureThat DSL)
ValueObjects/        Cross-cutting Value Objects (Code)
Auditing/            AuditLog (entity change model)
Features/<Feature>/  One folder per aggregate:
    <Aggregate>.cs           the aggregate / root entity
    <Aggregate>Errors.cs     strongly-typed errors (static class of Error)
    I<Feature>Repository.cs  repository interface (impl. in Infrastructure)
    ValueObjects/            feature-specific IDs and VOs (e.g., UserId, Email)
```

## Patterns and Practices

- **Strongly-typed identities**: every Id is a `record` inheriting from `TypedGuidId` with `New()` factory using `Guid.CreateVersion7()` (UUIDv7, sortable). Never use bare `Guid` as Id.
- **Aggregates/entities**: inherit from `AggregateRoot<TId>` / `Entity<TId>`. Private constructor + **static factory methods** (`User.Create(...)`) + business methods to mutate state. `Id` has `protected set`.
  - ⚠️ Encapsulation: the goal is `{ get; private set; }` with mutation only via business methods. Known debt exists (`User`/`Tenant` with public setters) — **don't imitate in new code**.
- **Value Objects**: `readonly record struct` or `sealed record`, immutable, with constructor validation (fail-fast) and implicit conversion operators when useful (see `Email`, `Code`).
- **Errors**: `static class XxxErrors` with `Error` properties (code + message). Handlers return `Result.Failure(XxxErrors.Something)`.
- **Result**: `Success` cannot coexist with an `Error` other than `Error.None`. Implicit conversion from `Error` exists.
- **Specifications**: encapsulate filters as `Expression<Func<T,bool>>` combinable with `And`/`Or`; translatable to SQL by EF Core. Use them for reusable query rules.
- **Validators**: `Validator<TCommand>` with `EnsureThat(predicate, error)` in constructor. Live in Application alongside their command, but use this DSL.
- **Repositories**: interface here, implementation in Infrastructure. Only for aggregate roots.
- **Domain events**: `AggregateRoot<TId>` exposes `DomainEvents` (read-only) and `ClearDomainEvents()`, and implements `IHasDomainEvents` so Infrastructure can collect them without knowing the generic type. Business methods call the `protected RaiseDomainEvent(IDomainEvent)` to register an event — never expose it publicly. Events are `sealed record`, named in the past tense (`PirateCrownedKing`), implement `IDomainEvent` (`OccurredOn`), and live in `Features/<Feature>/Events/`. Dispatch is post-commit and owned by Infrastructure (`DomainEventsDispatchInterceptor`); Domain only tracks pending events.

## Conventions

- Strict Object Calisthenics here: no `else`, one indentation per method, wrap primitives, no public getters/setters on aggregates.
- No `ILogger`, no unnecessary `async`, no infrastructure references.
- One type per file, file-scoped namespace `Raftel.Domain.*`.
