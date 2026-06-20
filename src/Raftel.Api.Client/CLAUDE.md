# Raftel.Api.Client

Client-side utilities for consuming the API. Deliberately minimal project: currently contains only `QueryFilter`, the helper that builds query parameters expected by AutoEndpoints queries.

## Structure

```
QueryFilter.cs   construction/serialization of query filters for HTTP API calls
```

## Patterns and Practices

- HTTP client support code: **no server dependencies** (doesn't reference Infrastructure or EF Core).
- Keep it symmetric with how `Raftel.Api.Server/AutoEndpoints` infers parameters from Query record constructor: what the server expects to read, this client must know how to write.
- As it grows, follow feature/responsibility organization and one type per file; don't put business logic or duplicated ad-hoc serialization here.
