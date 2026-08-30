## 1. Domain: PageRequest

- [x] 1.1 (test) `PageRequest.Create` rejects page 0, negative page, page size 0, and page size above `MaxPageSize`, returning `Error.Validation` in each case
- [x] 1.2 (test) `PageRequest.Create` accepts a valid page/page-size and exposes the correct `Skip`
- [x] 1.3 Implement `PageRequest` in `src/Raftel.Domain/Abstractions/PageRequest.cs`

## 2. Domain: PagedResult

- [x] 2.1 (test) `PagedResult.TotalPages` with a remainder (11 items, page size 5 ⇒ 3 pages)
- [x] 2.2 (test) `HasNext`/`HasPrevious` on first, intermediate, and last page
- [x] 2.3 (test) `PagedResult<T>.Empty` and `Map` produce the expected shape
- [x] 2.4 Implement `PagedResult<T>` in `src/Raftel.Domain/Abstractions/PagedResult.cs`

## 3. Domain: Sorting

- [x] 3.1 (test) `SortRequest.Parse` with `"name"`, `"-createdAt"`, `"a,-b"`, empty string, and an unparseable value
- [x] 3.2 Implement `SortDirection` in `src/Raftel.Domain/Abstractions/SortDirection.cs`
- [x] 3.3 Implement `SortRequest` in `src/Raftel.Domain/Abstractions/SortRequest.cs`
- [x] 3.4 (test) `SortMap<TEntity>` resolves an allowed field and rejects an unknown field with `Error.Validation` ("Sort.UnknownField")
- [x] 3.5 Implement `SortMap<TEntity>` in `src/Raftel.Domain/Specifications/SortMap.cs`

## 4. Repository: paged listing

- [x] 4.1 (test, Testcontainers) `EfRepository.ListPagedAsync` returns the correct page and correct `TotalCount`
- [x] 4.2 (test, Testcontainers) walking every page and unioning results yields the full set with no duplicates and no gaps (covers the stable tie-break)
- [x] 4.3 (test, Testcontainers) soft-delete and tenant global filters apply to both `LongCountAsync` and the page query
- [x] 4.4 Add `IRepository.ListPagedAsync` to `src/Raftel.Domain/Abstractions/IRepository.cs`
- [x] 4.5 Implement `ListPagedAsync` in `src/Raftel.Infrastructure/Data/EfRepository.cs`, including the mandatory `ThenBy(Id)` tie-break

## 5. Application: paged query contract

- [x] 5.1 Implement `IPagedQuery<TItem>` in `src/Raftel.Application/Queries/IPagedQuery.cs`
- [x] 5.2 Implement `PaginationOptions` in `src/Raftel.Application/PaginationOptions.cs` and register it via `IRaftelApplicationBuilder`

## 6. API: endpoint wiring

- [x] 6.1 Update `ApiParametersBuilder` to document `page`/`pageSize`/`sort` in OpenAPI when the request implements `IPagedQuery<>` — verified `ApiParametersBuilder.Calculate<TRequest>` already reflects over every public property of `TRequest`, so `Page`/`PageSize`/`Sort` on any `IPagedQuery<>` record are documented automatically with no code change needed
- [x] 6.2 Update `QueryEndpointMapper` (if needed) to confirm `page`/`pageSize`/`sort` bind correctly from route/query string for `IPagedQuery<>` requests — verified the existing constructor-based binding and `IsNullable()` fallback already handle `int?`/`string` paging parameters with no code change needed
- [x] 6.3 Emit `X-Total-Count` and `X-Total-Pages` response headers for paged endpoint responses
- [x] 6.4 (test, functional) `GET /api/tenants?page=2&pageSize=5&sort=-name` returns the correct page and `X-Total-Count` header

## 7. Migrate GetAllTenantsQuery

- [x] 7.1 Migrate `GetAllTenantsQuery`/handler in `src/Raftel.Application/Features/Tenants/GetAllTenants/*` to `IPagedQuery<TenantResponse>`
- [x] 7.2 Document the `GET /api/tenants` response-shape change in `BREAKING_CHANGES.md`

## 8. Client helper

- [x] 8.1 (test) `QueryFilter.Paged(page, pageSize, sort)` serializes `page`/`pageSize`/`sort` correctly
- [x] 8.2 Implement `QueryFilter.Paged` extension in `src/Raftel.Api.Client/QueryFilter.cs`

## 9. Demo and docs

- [x] 9.1 Add at least one paginated query example in `demo/**`, documented as the pattern to follow
- [x] 9.2 Verify all acceptance criteria in `docs/backlog/03-paginacion-y-ordenacion.md` §6 are met
