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
    Interceptors/                    AuditPropertiesInterceptor, TenantInterceptor, EntityChangesTrackerInterceptor, DomainEventsDispatchInterceptor, OutboxWriteInterceptor
    Repositories/<Feature>/          UsersRepository, TenantsRepository, RolesRepository
Authentication/                      AuthenticationService, CurrentHttpUser, ClaimsPrincipalFactory, RaftelClaimTypes
Multitenancy/                        CurrentTenant (AsyncLocal) + Middleware/TenantMiddleware
Correlation/                         CorrelationContext (ICorrelationContext impl., IHttpContextAccessor-backed) + CorrelationScope (AsyncLocal override) + CorrelationIdSanitizer
Outbox/                              OutboxStore (claim = one provider-specific UPDATE...OUTPUT/RETURNING), EfIdempotencyStore, OutboxSerializer, OutboxMessageDispatcher, OutboxBatchRunner, OutboxProcessor, OutboxCleanupService
DependencyInjection.cs               AddRaftelData<TDbContext>(configuration, connectionStringName)
```

## Patterns and Practices

- **DbContext**: apps derive from `RaftelDbContext<TDbContext>`. Global filters (soft-delete + multitenancy) are applied via reflection on shadow properties and **combine** with any existing entity filter (`CombineQueryFilter`) — they don't overwrite.
- **Repositories**: inherit from `EfRepository<TDbContext, TAggregate, TId>` and implement Domain interface. Most remain empty; add methods only for aggregate-specific queries.
- **Interceptors** (EF `SaveChanges`): automate audit logging (creator/date), `TenantId` assignment, and change tracking without touching handlers. Registered and hooked in `AddDbContext`.
- **Domain events dispatch**: `DomainEventsDispatchInterceptor` runs in `SavedChanges`/`SavedChangesAsync` (post-commit, not `SavingChanges`) — events only reach `IDomainEventsDispatcher` (Application) if the transaction actually committed. It reads `ChangeTracker.Entries<IHasDomainEvents>()` (the non-generic marker from Domain) instead of the generic `AggregateRoot<TId>`, collects `DomainEvents`, calls `ClearDomainEvents()`, then dispatches. It never mutates entities.
- **Outbox** (opt-in, `configureOutbox: o => o.Enabled = true` on `AddRaftelData`): `AddDataAccess` attaches `OutboxWriteInterceptor` *instead of* `DomainEventsDispatchInterceptor`, so the disabled path is the original code. The interceptor stores events in `SavingChanges` (same transaction); `OutboxProcessor` (hosted) claims messages and `OutboxMessageDispatcher` runs each handler in its own scope + transaction under the message's tenant and correlation id, with a receipt row committed alongside the handler's writes. Things that are easy to get wrong:
  - **Attempts are counted at claim**, not on failure, so a message that crashes the process still dead-letters. `LockedUntil` is the lease *and* the EF concurrency token: an outcome saved after the lease was lost is discarded (`IOutboxStore.SaveAsync` returns `false`).
  - The claim runs as raw ADO.NET, **not** `FromSql`: `UPDATE ... OUTPUT`/`RETURNING` cannot be wrapped in a subquery.
  - `OutboxMessage.TenantId` matches the shadow-property name convention, so it gets the tenant global filter: every store query uses `IgnoreQueryFilters()`.
  - Event JSON goes through `OutboxSerializer`: typed ids need `TypedGuidIdJsonConverterFactory`, and get-only `OccurredOn { get; } = UtcNow` is restored through its backing field. Stored type names are looked up in `IDomainEventTypeRegistry`, never loaded with `Type.GetType`.
  - Outbox tables are mapped even when the outbox is disabled (the model must not depend on a runtime flag).
- **Disableable filters**: `IDataFilter.Disable<TFilter>()` temporarily disables a filter with `AsyncLocal` + `IDisposable` (`using`), for admin tasks/jobs. Don't mutate global state.
- **End-to-end multitenancy**: header `X-Tenant-Id` → `TenantMiddleware` → `ICurrentTenant` (AsyncLocal) → `TenantInterceptor` (writes) → query filter (reads).
- **Auth**: ASP.NET Identity + OpenIddict (password flow + refresh token), self-hosted. Token endpoint at `/connect/token`.
- **Multi-provider**: SQL Server and PostgreSQL selected via `DatabaseOptions.Provider`.

## Conventions

- All cross-layer dependencies registered here via `AddRaftelData<TDbContext>`; apps invoke it from their own `DependencyInjection`.
- **No** business logic here: only persistence, Value Object mapping (conversions in `Configuration/`), and integrations.
- Object Calisthenics relaxed for infrastructure code, but maintain one type per file and file-scoped namespaces.
