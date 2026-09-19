# Raftel.Application

Application layer (CQRS). Implements a **custom mediator** (no MediatR) that resolves handlers and dynamically composes the middleware pipeline. **Depends only on `Raftel.Domain`.** Contains no business logic: orchestrates.

## Structure

```
Abstractions/
    IRequest / IRequestHandler / IRequestDispatcher / RequestDispatcher   base mediator
    RequestHandlerDelegate                                                pipeline link
    IRequestEvent / RequestEventFields / RequestEvent                     request-scoped wide event (see below)
    ICorrelationContext                                                   correlation id for the current request (impl. in Infrastructure)
    Authentication/   ICurrentUser, IAuthenticationService (interfaces, impl. in Infrastructure)
    Multitenancy/     ICurrentTenant
    DomainEvents/     IDomainEventHandler<TEvent>, IDomainEventsDispatcher + DomainEventsDispatcher impl.
Commands/             ICommand, ICommand<TResult>, ICommandHandler<,>, ICommandDispatcher, CommandDispatcher
Queries/              IQuery<TResult>, IQueryHandler<,>, IQueryDispatcher, QueryDispatcher
Middlewares/          IGlobalMiddleware / ICommandMiddleware / IQueryMiddleware + implementations + MiddlewareRegistry
Authorization/        RequiresPermissionAttribute
Exceptions/           ValidationException, UnauthorizedException
Features/<Feature>/<UseCase>/   one use case per folder (see below)
DependencyInjection.cs          AddRaftelApplication(cfg => ...)
RaftelApplicationBuilder.cs     assembly and middleware registration
```

## Anatomy of a Use Case (Vertical Feature)

Each use case is a folder `Features/<Feature>/<UseCase>/` grouping everything:

- **Command** (mutation): `record CreateUserCommand(...) : ICommand;` or `: ICommand<TResult>` if returning a value. Decorate with `[RequiresPermission(...)]` if requiring authorization.
- **Query** (read): `record GetUserProfileQuery(...) : IQuery<GetUserProfileResponse>;`
- **Handler**: `sealed class ...Handler(deps) : ICommandHandler<TCommand>` / `IQueryHandler<TQuery,TResponse>`, `HandleAsync` method. Returns `Result`/`Result<T>`.
- **Validator** (optional): `class ...Validator : Validator<TCommand>` with `EnsureThat(...)`. Auto-discovered and registered via reflection.
- **Response**: output record/DTO (DTOs can have public setters).
- **`<Feature>Permissions.cs`**: feature permission constants.

## Pipeline and Mediator

- `RequestDispatcher` reflects to detect if request is `ICommand`/`ICommand<T>`/`IQuery<T>` and chains: `IGlobalMiddleware` + command/query-specific middlewares, ending with handler.
- Included middlewares: `ValidationMiddleware` (runs `Validator<T>`), `PermissionAuthorizationMiddleware` (via `[RequiresPermission]`), `TransactionMiddleware` (command-only, opens/commits/rolls back an explicit transaction — see below), `UnitOfWorkMiddleware` (commits **only if result succeeds**), `LoggingMiddleware` (wide-event enrichment — see below).
- To add new cross-cutting behavior (caching, logging…), create a middleware of the appropriate type and register it in `AddRaftelApplication`.
- Global middleware order is caller-defined by the order of `AddGlobalMiddleware` calls. **`LoggingMiddleware<,>` must be registered first** so it wraps every other global middleware and captures their outcome too.
- Command middleware order is likewise caller-defined by `AddCommandMiddleware` call order, outermost first. **`TransactionMiddleware<>`/`TransactionMiddleware<,>` must be registered before `UnitOfWorkMiddleware<>`/`UnitOfWorkMiddleware<,>`** so the transaction wraps the commit (and, through it, auditing and domain event dispatch — both ride on `SaveChanges`). `TransactionMiddleware` is opt-in, like `UnitOfWorkMiddleware`: an app registers it explicitly if it wants command handling wrapped in an explicit, rollback-capable transaction instead of a bare `SaveChanges`.

## Wide Events (Structured Logging)

Raftel logs one structured "wide event" (canonical log line) per HTTP request, not scattered per-line logs — see [GitHub issue #115](https://github.com/franciscofsl/Raftel/issues/115). The event is a request-scoped field bag (`IRequestEvent`), enriched throughout the request and emitted **exactly once** by `CorrelationIdMiddleware` in `Raftel.Api.Server` (the outermost middleware, so it also covers requests that never reach the command/query pipeline).

- **`LoggingMiddleware<TRequest, TResponse>`** enriches the event with `request_name` (`typeof(TRequest).Name`, never request property values — commands routinely carry passwords/tokens) and the outcome: nothing extra on success, `level = Warning` + `error.code`/`error.message` on a failed `Result` (expected business flow, not a defect), or `level = Error` + the exception on a thrown exception (which is then rethrown unchanged). It never logs directly.
- **Enriching from a handler**: inject `IRequestEvent` and call `Set("field.name", value)` / `Increment("field.name")` for domain context worth debugging with (e.g. `pirate.id`, `db.query_count`). Expose field names as a `<Feature>EventFields` constants class (mirroring `<Feature>Permissions`) rather than inline string literals, so additions are reviewable in a PR diff.
- **Deny-list**: `IRequestEvent.Set`/`Increment` silently drop any field whose name contains `password`, `token`, `secret`, `apikey`, or `authorization` (case-insensitive) — never throws, never breaks the request. This is a safety net, not a substitute for not enriching with sensitive data in the first place.
- Do not add ad hoc logging elsewhere in the pipeline, and do not serialize a request/command object into the event — enrichment is always explicit, field by field.

## Domain Events

- `IDomainEventHandler<TEvent>` is a side-effect handler for one `IDomainEvent`; implement one per use case, e.g. `Features/<Feature>/Events/<EventName>Handler.cs`.
- `DomainEventsDispatcher` resolves all `IDomainEventHandler<TConcrete>` for a given event from `IServiceProvider` and invokes them — zero handlers is a no-op, multiple handlers all run.
- Dispatch itself is triggered by Infrastructure (`DomainEventsDispatchInterceptor`, post-commit), never by a command handler directly — handlers stay thin and don't know about events being raised.
- With `TransactionMiddleware` registered, a domain event handler's own database writes (repository call + `IUnitOfWork.CommitAsync()`) join the command's still-open transaction and roll back with it — see the XML doc on `IDomainEventHandler` for the exact guarantee and its limits.

## Conventions

- Handlers `sealed`; max ~2 instance dependencies (Object Calisthenics — logger doesn't count).
- No EF Core or infrastructure types: only interfaces (`IRepository`, `ICurrentUser`, `IUnitOfWork`).
- Auto-discovery: handlers, validators, and `IDomainEventHandler<>` are discovered by assembly in `RegisterServicesFromAssembly(...)`. Don't register manually.
- `InternalsVisibleTo("Raftel.Application.UnitTests")` is active; handlers can be `internal sealed`.
