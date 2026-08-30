## Why

`IRepository.ListAllAsync` materializes the entire table with no limit — `GetAllTenantsQuery` returns every tenant, and with 100k rows the API falls over. The solution has no `PagedResult`, `PageRequest`, or `Sort` types anywhere. `Raftel.Api.Client.QueryFilter` already knows how to build query strings from an object (`FromObject`), but nothing server-side consumes `page`, `pageSize`, or `sort` — the client half of the contract exists without its server counterpart.

## What Changes

- Add `PageRequest` (validated, 1-based page, bounded page size) and `PagedResult<T>` (with `Map`) to `Raftel.Domain.Abstractions`.
- Add `SortRequest`/`SortDirection` with a JSON:API-style `-field` parsing convention, and `SortMap<TEntity>` — a per-entity allow-list that resolves a client-supplied field name to an `Expression<Func<TEntity, object>>`, rejecting unknown fields with `Error.Validation` instead of building unsafe dynamic expressions.
- Add `IRepository.ListPagedAsync(PageRequest, filter?, sort?, cancellationToken)` and implement it in `EfRepository`: count + page in two queries (portable across SqlServer/PostgreSQL), with a mandatory stable tie-break (`ThenBy(Id)`) so `Skip/Take` never drifts across pages.
- Add `IPagedQuery<TItem>` to `Raftel.Application` so queries can declare `Page`/`PageSize`/`Sort` and get them bound automatically from route/query string via the existing `QueryEndpointMapper`.
- Add `PaginationOptions` (`DefaultPageSize`, `MaxPageSize`) registered through `IRaftelApplicationBuilder`.
- Document `page`/`pageSize`/`sort` in OpenAPI for `IPagedQuery<>` requests, and emit optional `X-Total-Count`/`X-Total-Pages` response headers.
- Add a `Raftel.Api.Client.QueryFilter.Paged(...)` helper symmetric with the existing `FromObject`.
- **BREAKING** (endpoint contract only, not the framework): migrate `GetAllTenantsQuery` from an unbounded list response to `IPagedQuery<TenantResponse>` — the `GET /api/tenants` response shape changes from a bare array to a paged envelope.
- `ListAllAsync` is kept as-is for small catalogs; no framework-level breaking change.

## Capabilities

### New Capabilities
- `pagination-and-sorting`: Framework-level paging (`PageRequest`, `PagedResult<T>`), sorting (`SortRequest`, `SortMap<TEntity>`), the `IRepository.ListPagedAsync` contract, `IPagedQuery<TItem>` query binding, `PaginationOptions`, OpenAPI documentation of paging/sort parameters, and the client-side `QueryFilter.Paged` helper.

### Modified Capabilities
(none — no existing specs describe `GetAllTenantsQuery`'s current unbounded behavior; its migration is covered as part of the new capability's acceptance criteria and as an example consumer, not as a delta to a pre-existing spec.)

## Impact

- **New**: `src/Raftel.Domain/Abstractions/{PageRequest,PagedResult,SortRequest,SortDirection}.cs`, `src/Raftel.Domain/Specifications/SortMap.cs`, `src/Raftel.Application/Queries/IPagedQuery.cs`, `src/Raftel.Application/PaginationOptions.cs`.
- **Modified**: `src/Raftel.Domain/Abstractions/IRepository.cs`, `src/Raftel.Infrastructure/Data/EfRepository.cs`, `src/Raftel.Api.Server/AutoEndpoints/{ApiParametersBuilder,QueryEndpointMapper}.cs`, `src/Raftel.Application/Features/Tenants/GetAllTenants/*`, `src/Raftel.Api.Client/QueryFilter.cs`, `demo/**` (one paginated example query).
- **Tests**: new unit tests (`Raftel.Domain.Tests`), integration tests against Testcontainers (`Raftel.Infrastructure.Tests`), and functional HTTP contract tests (`Raftel.Api.FunctionalTests`).
- **Depends on**: change `error-typed-http-mapping` (typed `Error`/`Result` used for validation failures) and cancellation-token propagation (backlog item 02) — paging queries must accept and forward a `CancellationToken`.
- **Consumers**: any endpoint returning unbounded collections should eventually migrate to `IPagedQuery<>`; only `GetAllTenantsQuery` is migrated as part of this change, documented in `BREAKING_CHANGES.md`.
