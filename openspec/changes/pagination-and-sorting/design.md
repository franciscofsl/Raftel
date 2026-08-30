## Context

See proposal.md - Why. Key constraints from the backlog write-up (`docs/backlog/03-paginacion-y-ordenacion.md`):
- `Raftel.Domain` cannot depend on EF Core or HTTP — paging/sort primitives must be plain records.
- `Raftel.Api.Client.QueryFilter.FromObject` already builds query strings from an object; the server side has nothing that consumes `page`/`pageSize`/`sort` yet.
- `QueryEndpointMapper.BuildRequestFromRouteAndQuery` already binds constructor parameters from route/query string, so a query record that declares `Page`/`PageSize`/`Sort` gets them bound with no mapper changes.
- Must support both SqlServer and PostgreSQL without provider-specific SQL.

## Goals / Non-Goals

**Goals:**
- Deterministic, duplicate-free, gap-free paging under concurrent inserts/deletes between page fetches (best-effort via stable tie-break, not full snapshot isolation).
- Reject invalid/oversized/unknown-field input at the boundary (`400` + `Error.Validation`), never silently clamp or ignore.
- Zero new provider-specific SQL in the repository layer.
- One migrated example consumer (`GetAllTenantsQuery`) so the pattern is proven end-to-end, not just added as unused library code.

**Non-Goals:**
- Keyset/cursor pagination (noted as a possible future *additional* capability in the proposal's alternatives, not built here).
- Migrating every existing unbounded query — only `GetAllTenantsQuery`.
- Full snapshot isolation across page fetches (out of scope; two-query count+page approach can still observe concurrent writes between the count and the page query — see Risks).

## Decisions

**Two queries (count + page) instead of `COUNT(*) OVER()` window function.**
Rationale: window functions require provider-specific SQL dialects to translate identically well; two plain LINQ queries (`LongCountAsync` + `Skip/Take`) translate portably to both SqlServer and PostgreSQL through EF Core. Both queries run against the same `DbContext`, so they share the same global query filters (soft delete, tenant). Trade-off: two round-trips instead of one, and a small window where count and page can disagree under concurrent writes (see Risks).

**Mandatory stable tie-break (`ThenBy(Id)`) appended to every paged query, regardless of client-requested sort.**
Rationale: `Skip/Take` without a fully deterministic `ORDER BY` can repeat or omit rows across page fetches when the natural row order isn't stable (common on both SqlServer and PostgreSQL without an explicit order). Appending the primary key as the final sort key guarantees a total order. Alternative considered: trust the database's default order — rejected, it's an implementation detail that isn't guaranteed stable across pages.

**Sort field resolution via an explicit per-entity allow-list (`SortMap<TEntity>`) rather than reflection-driven dynamic sorting.**
Rationale: `SortRequest.Field` is client-controlled input. Building `OrderBy` via string interpolation or unrestricted reflection is an injection surface and also leaks internal property names as a public sort contract. An allow-list makes the sortable surface an explicit, reviewable declaration per entity and gives a clean `Error.Validation` path for unknown fields. Alternative considered: allow sorting by any public property via reflection — rejected for the injection/surface-area reasons above.

**`-field` prefix convention for descending sort (JSON:API-style) instead of a separate `sortDirection` parameter.**
Rationale: keeps the query string to one parameter (`sort=-createdAt,name`) instead of two parallel arrays that must be kept in sync client-side. Trade-off: field names starting with a literal `-` are impossible to express, which is acceptable since field names are identifiers, not arbitrary strings.

**`ListAllAsync` is kept, not removed or renamed.**
Rationale: still useful for small, bounded catalogs where paging is overhead. Only `ListPagedAsync` is added as a new, separate repository method. The backlog's suggestion to mark the no-filter overload `[Obsolete]` is left as a documentation note rather than an attribute, to avoid a breaking compiler warning across the whole solution as part of this change.

**`IPagedQuery<TItem>` extends the existing `IQuery<PagedResult<TItem>>` with nullable `Page`/`PageSize`/`Sort` properties**, relying on `QueryEndpointMapper`'s existing constructor-parameter binding from route/query string rather than introducing new binding infrastructure.
Rationale: minimizes new surface in `Raftel.Api.Server`; the mapper already does the work needed. `ApiParametersBuilder` only needs to special-case `IPagedQuery<>` to document the three parameters in OpenAPI.

**`GetAllTenantsQuery` migration is a genuine breaking change to that one endpoint's response shape** (bare array → paged envelope), not hidden behind a compatibility shim.
Rationale: per CLAUDE.md, no backwards-compatibility hacks; the change is small in scope (one endpoint) and is explicitly documented in `BREAKING_CHANGES.md` rather than papered over.

## Risks / Trade-offs

- **[Risk]** Two separate queries (count, then page) can observe different snapshots under concurrent writes — a row inserted between the count and the page query can shift page boundaries. → **Mitigation**: acceptable for a numbered-pager UI (not a real-time feed); documented as a known limitation, not solved by this change. Cursor/keyset pagination (deferred, see proposal's Alternatives) would close this gap for high-churn tables.
- **[Risk]** `MaxPageSize` set too low by an application via `PaginationOptions` could break existing UIs that expect larger pages. → **Mitigation**: configurable per-application with a sane framework default (200); enforced consistently so violations surface as `400` during development, not silently.
- **[Risk]** `SortMap<TEntity>` allow-lists must be authored per entity; a forgotten entity means sorting simply isn't available for it (fails safe) but requires a developer to remember to add the map. → **Mitigation**: unknown-field resolution returns a clear `Error.Validation`, not a silent no-op, making the gap discoverable immediately during integration/functional testing.
- **[Trade-off]** Response headers (`X-Total-Count`, `X-Total-Pages`) duplicate information already in the paged body. → **Mitigation**: kept optional/additive so clients that only read the body are unaffected; useful for clients doing HEAD-style total lookups or generic paging UI components.

## Migration Plan

1. Add domain/application types first (`PageRequest`, `PagedResult<T>`, `SortRequest`, `SortDirection`, `SortMap<TEntity>`, `IPagedQuery<TItem>`, `PaginationOptions`) — purely additive, no existing code touched.
2. Add `IRepository.ListPagedAsync` and its `EfRepository` implementation — additive; `ListAllAsync` untouched.
3. Wire `ApiParametersBuilder`/`QueryEndpointMapper` support for `IPagedQuery<>` — additive to the endpoint pipeline.
4. Migrate `GetAllTenantsQuery` to `IPagedQuery<TenantResponse>` last, once the underlying primitives are tested — this is the one breaking step; document the response-shape change in `BREAKING_CHANGES.md` in the same commit.
5. No rollback tooling needed beyond standard git revert — no data migration, no schema change, purely code-level.
