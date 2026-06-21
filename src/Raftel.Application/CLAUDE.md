# Raftel.Application

Application layer (CQRS). Implements a **custom mediator** (no MediatR) that resolves handlers and dynamically composes the middleware pipeline. **Depends only on `Raftel.Domain`.** Contains no business logic: orchestrates.

## Structure

```
Abstractions/
    IRequest / IRequestHandler / IRequestDispatcher / RequestDispatcher   base mediator
    RequestHandlerDelegate                                                pipeline link
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
- Included middlewares: `ValidationMiddleware` (runs `Validator<T>`), `PermissionAuthorizationMiddleware` (via `[RequiresPermission]`), `UnitOfWorkMiddleware` (commits **only if result succeeds**).
- To add new cross-cutting behavior (caching, logging…), create a middleware of the appropriate type and register it in `AddRaftelApplication`.

## Domain Events

- `IDomainEventHandler<TEvent>` is a side-effect handler for one `IDomainEvent`; implement one per use case, e.g. `Features/<Feature>/Events/<EventName>Handler.cs`.
- `DomainEventsDispatcher` resolves all `IDomainEventHandler<TConcrete>` for a given event from `IServiceProvider` and invokes them — zero handlers is a no-op, multiple handlers all run.
- Dispatch itself is triggered by Infrastructure (`DomainEventsDispatchInterceptor`, post-commit), never by a command handler directly — handlers stay thin and don't know about events being raised.

## Conventions

- Handlers `sealed`; max ~2 instance dependencies (Object Calisthenics — logger doesn't count).
- No EF Core or infrastructure types: only interfaces (`IRepository`, `ICurrentUser`, `IUnitOfWork`).
- Auto-discovery: handlers, validators, and `IDomainEventHandler<>` are discovered by assembly in `RegisterServicesFromAssembly(...)`. Don't register manually.
- `InternalsVisibleTo("Raftel.Application.UnitTests")` is active; handlers can be `internal sealed`.
