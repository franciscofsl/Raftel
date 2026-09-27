# Raftel.Infrastructure

Technical implementations: EF Core, repositories, multitenancy, audit logging, and authentication. **Only framework layer that knows EF Core.** Depends on `Application` and `Domain`; implements interfaces they declare.

## Structure

```
Data/
    RaftelDbContext<TDbContext>      base DbContext: applies composable global filters (soft-delete + tenant)
    EfRepository<TDbContext,T,TId>   generic IRepository implementation
    AuditStore.cs                    entity change log persistence
    ShadowPropertyNames.cs           shadow property names (TenantId, IsDeleted, audit...)
    DatabaseOptions / DatabaseProvider   SqlServer | PostgreSql
    Configuration/                   IEntityTypeConfiguration per framework entity (User, Role, Tenant, Permission)
    Filters/                         IDataFilter + ISoftDeleteFilter/ITenantFilter + QueryFilterExtensions
    Interceptors/                    AuditPropertiesInterceptor, TenantInterceptor, EntityChangesTrackerInterceptor, DomainEventsDispatchInterceptor
    Repositories/<Feature>/          UsersRepository, TenantsRepository, RolesRepository
Authentication/                      AuthenticationService, CurrentHttpUser, ClaimsPrincipalFactory, RaftelClaimTypes
Multitenancy/                        CurrentTenant (AsyncLocal) + Middleware/TenantMiddleware
Correlation/                         CorrelationContext (ICorrelationContext impl., IHttpContextAccessor-backed) + CorrelationIdSanitizer
Health/                              DatabaseHealthCheck, PendingMigrationsHealthCheck, StartupHealthCheck(+StartupHostedService),
                                      TenantResolutionHealthCheck, OutboxHealthCheck, IDatabaseProbe(+DatabaseProbe<TDbContext>),
                                      HealthCheckNames, HealthDependencyInjection.AddRaftelHealthChecks(options)
DependencyInjection.cs               AddRaftelData<TDbContext>(configuration, connectionStringName)
```

## Patterns and Practices

- **DbContext**: apps derive from `RaftelDbContext<TDbContext>`. Global filters (soft-delete + multitenancy) are applied via reflection on shadow properties and **combine** with any existing entity filter (`CombineQueryFilter`) — they don't overwrite.
- **Repositories**: inherit from `EfRepository<TDbContext, TAggregate, TId>` and implement Domain interface. Most remain empty; add methods only for aggregate-specific queries.
- **Interceptors** (EF `SaveChanges`): automate audit logging (creator/date), `TenantId` assignment, and change tracking without touching handlers. Registered and hooked in `AddDbContext`.
- **Domain events dispatch**: `DomainEventsDispatchInterceptor` runs in `SavedChanges`/`SavedChangesAsync` (post-commit, not `SavingChanges`) — events only reach `IDomainEventsDispatcher` (Application) if the transaction actually committed. It reads `ChangeTracker.Entries<IHasDomainEvents>()` (the non-generic marker from Domain) instead of the generic `AggregateRoot<TId>`, collects `DomainEvents`, calls `ClearDomainEvents()`, then dispatches. It never mutates entities.
- **Disableable filters**: `IDataFilter.Disable<TFilter>()` temporarily disables a filter with `AsyncLocal` + `IDisposable` (`using`), for admin tasks/jobs. Don't mutate global state.
- **End-to-end multitenancy**: header `X-Tenant-Id` → `TenantMiddleware` → `ICurrentTenant` (AsyncLocal) → `TenantInterceptor` (writes) → query filter (reads).
- **Auth**: ASP.NET Identity + OpenIddict (password flow + refresh token), self-hosted. Token endpoint at `/connect/token`.
- **Multi-provider**: SQL Server and PostgreSQL selected via `DatabaseOptions.Provider`.
- **Health checks**: `AddRaftelHealthChecks(options)` registers the mandatory `database`/`migrations`/`startup` checks (tagged `ready`) plus opt-in `tenants`/`outbox` checks; endpoint mapping lives in `Raftel.Api.Server/Health` (see `docs/health-checks.md`). `HealthOptions` and the `ready` tag live in `Raftel.Application.Abstractions.Health` — not here — so `Raftel.Api.Server` can read them without depending on `Raftel.Infrastructure` (the architecture tests forbid that dependency). Every check must honor its `CancellationToken` (so `HealthOptions.CheckTimeout` actually bounds it) and must never put an exception message, stack trace, or connection string into its `Description`/`Data` — catch internally and return `HealthCheckResult.Unhealthy("<safe text>")` instead; the real exception belongs in structured logs, not the health response body.

## Conventions

- All cross-layer dependencies registered here via `AddRaftelData<TDbContext>`; apps invoke it from their own `DependencyInjection`.
- **No** business logic here: only persistence, Value Object mapping (conversions in `Configuration/`), and integrations.
- Object Calisthenics relaxed for infrastructure code, but maintain one type per file and file-scoped namespaces.
