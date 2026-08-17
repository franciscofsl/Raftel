## Purpose

Gives the framework a bounded, deterministic way to page and sort collections end-to-end — from repository query through query handler to HTTP response and back into the typed client — so no framework query can silently return an unbounded result set.

## ADDED Requirements

### Requirement: Page request validation
The system SHALL validate page and page-size inputs before they reach any query, rejecting invalid values with a validation error rather than silently clamping or ignoring them.

#### Scenario: Page below one is rejected
- **WHEN** a page request is created with `page = 0` or a negative page
- **THEN** the system returns a validation failure and does not execute a query

#### Scenario: Page size out of bounds is rejected
- **WHEN** a page request is created with `pageSize < 1` or `pageSize` greater than the configured maximum
- **THEN** the system returns a validation failure and does not silently clamp the value

#### Scenario: Valid page request is accepted
- **WHEN** a page request is created with `page >= 1` and `1 <= pageSize <= MaxPageSize`
- **THEN** the system produces a usable page request with the given page and page size

### Requirement: Paged result shape
The system SHALL represent a page of results with the items returned, the requesting page and page size, and the total count of matching items across all pages.

#### Scenario: Total pages computed from total count
- **WHEN** a paged result is built with 11 total matching items and a page size of 5
- **THEN** the reported total page count is 3

#### Scenario: Previous/next availability at boundaries
- **WHEN** a paged result is on the first page
- **THEN** it reports no previous page available
- **WHEN** a paged result is on the last page
- **THEN** it reports no next page available
- **WHEN** a paged result is on an intermediate page
- **THEN** it reports both a previous and a next page available

#### Scenario: Empty result set
- **WHEN** a query matches zero items
- **THEN** the system returns a paged result with an empty item list and a total count of zero, without querying for items

### Requirement: Sort expression parsing
The system SHALL parse a client-supplied sort string into an ordered list of field/direction pairs, using a leading `-` on a field name to indicate descending order and its absence to indicate ascending order.

#### Scenario: Single ascending field
- **WHEN** the sort string is `"name"`
- **THEN** the system parses one sort entry for field `name` in ascending order

#### Scenario: Single descending field
- **WHEN** the sort string is `"-createdAt"`
- **THEN** the system parses one sort entry for field `createdAt` in descending order

#### Scenario: Multiple comma-separated fields
- **WHEN** the sort string is `"a,-b"`
- **THEN** the system parses two sort entries, in order: field `a` ascending, field `b` descending

#### Scenario: Empty or absent sort string
- **WHEN** the sort string is empty or not provided
- **THEN** the system parses to an empty ordered list and applies no client-requested ordering

### Requirement: Sortable field allow-listing
The system SHALL restrict which fields a client can sort by to an explicit, per-entity allow-list, and SHALL reject any sort field not on that list with a validation error rather than ignoring it or building an unvalidated dynamic expression.

#### Scenario: Allowed field resolves
- **WHEN** a sort field that has been explicitly allowed for an entity is resolved
- **THEN** the system produces the expression to sort by, without string-interpolating client input into a query

#### Scenario: Unknown field is rejected
- **WHEN** a sort field that has not been declared allowed for an entity is resolved
- **THEN** the system returns a validation error and does not perform an unordered or silently-ignored sort

### Requirement: Repository paged listing
The system SHALL provide a repository operation that returns a page of entities matching an optional filter, ordered by an optional client-requested sort, alongside the total count of matching entities — without ever materializing the full unfiltered/unpaged result set into memory.

#### Scenario: Correct page and total count
- **WHEN** a paged listing is requested for a given page, page size, and optional filter
- **THEN** the returned page contains at most `pageSize` items belonging to that page, and the total count reflects all matching entities, not just the returned page

#### Scenario: Stable ordering across pages
- **WHEN** every page of a paged listing is retrieved in sequence, with or without a client-requested sort
- **THEN** the union of all pages' items equals the full matching set exactly once each, with no duplicates and no omissions

#### Scenario: Global filters apply to both count and page
- **WHEN** a paged listing is requested against an entity subject to soft-delete and/or multitenancy filters
- **THEN** both the reported total count and the returned page reflect those global filters

### Requirement: Query-level paging contract
The system SHALL let a query declare that it returns a paged result and accepts `page`, `pageSize`, and `sort` inputs, binding those inputs from the incoming request without additional per-endpoint wiring.

#### Scenario: Paged query bound from request
- **WHEN** an HTTP request for a paged query includes `page`, `pageSize`, and `sort` values
- **THEN** the query handler receives those values and returns a paged result reflecting them

#### Scenario: Paged query defaults when inputs omitted
- **WHEN** an HTTP request for a paged query omits `page`, `pageSize`, or `sort`
- **THEN** the system applies the configured default page size, defaults to the first page, and applies no additional client-requested ordering

### Requirement: Configurable pagination limits
The system SHALL allow the default page size and the maximum allowed page size to be configured at the application level, and SHALL enforce the configured maximum for every paged query.

#### Scenario: Request exceeding configured maximum page size is rejected
- **WHEN** a client requests a page size greater than the configured maximum
- **THEN** the system returns a validation error rather than serving a larger page or silently truncating the requested size

### Requirement: Paging and sort parameters documented in OpenAPI
The system SHALL document `page`, `pageSize`, and `sort` as parameters in the generated OpenAPI description of any endpoint backed by a paged query.

#### Scenario: OpenAPI describes paging parameters
- **WHEN** the OpenAPI description is generated for an endpoint backed by a paged query
- **THEN** it includes `page`, `pageSize`, and `sort` as documented parameters for that endpoint

### Requirement: Total count exposed on paged HTTP responses
The system SHALL make the total matching count and total page count available on the HTTP response of a paged endpoint, in addition to the response body.

#### Scenario: Total count header present on paged response
- **WHEN** a client calls an endpoint backed by a paged query
- **THEN** the HTTP response includes the total matching count and total page count as response headers, alongside the paged body

### Requirement: Client-side paging request construction
The system SHALL let API client callers build a query filter for `page`, `pageSize`, and `sort` symmetrically with the existing client-side query-filter construction from an object.

#### Scenario: Client builds a paged query filter
- **WHEN** a client constructs a query filter with a page, a page size, and an optional sort string
- **THEN** the resulting query filter serializes `page`, `pageSize`, and `sort` as it would for any other query-filter parameter
