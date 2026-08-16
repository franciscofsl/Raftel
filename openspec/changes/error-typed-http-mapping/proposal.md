## Why

`Error` today is `record Error(string Code, string Message)` with no semantic
category, and both `CommandEndpointMapper` and `QueryEndpointMapper` collapse
every failed `Result` to `400 Bad Request`:

```csharp
return result.IsSuccess
    ? Results.Ok()
    : Results.BadRequest(result.Error);
```

A missing aggregate returns `400` instead of `404`, a uniqueness conflict
returns `400` instead of `409`, and clients cannot distinguish "malformed
request" from "business rule violated." The error body is also a plain
serialized `Error`, while `ExceptionHandlingMiddleware` already returns RFC
7807 `ProblemDetails` — two different error shapes in the same API. This is
the first item in the framework backlog: paging, validation-as-Result, and
caching all return errors and need this taxonomy to already be stable.

## What Changes

- Add `ErrorType` enum (`Failure`, `Validation`, `NotFound`, `Conflict`,
  `Unauthorized`, `Forbidden`, `Unexpected`) in `Raftel.Domain.Abstractions`,
  with no HTTP knowledge — the domain stays transport-agnostic.
- Extend `Error` with a `Type` property (default `Failure`, so the existing
  two-arg constructor keeps compiling) and typed factory methods
  (`Error.Validation`, `Error.NotFound`, `Error.Conflict`, `Error.Forbidden`,
  `Error.Unauthorized`, `Error.Failure`).
- **BREAKING**: `Error.None` and `Error.NullValue` change from mutable
  `public static` fields to `static readonly` — closes a latent bug where
  either could be reassigned.
- Add `ErrorResults.ToProblem(Error)` in `Raftel.Api.Server` as the single
  translation point from `Error` to an HTTP `ProblemDetails` response, mapping
  `ErrorType` to status code (`Validation`→400, `NotFound`→404, `Conflict`→409,
  `Unauthorized`→401, `Forbidden`→403, `Unexpected`→500).
- Wire `ErrorResults` into `CommandEndpointMapper` and `QueryEndpointMapper`
  instead of the current `Results.BadRequest(result.Error)`.
- **BREAKING**: success codes change from a hardcoded `200 OK` to
  method-appropriate codes: `POST`/`PUT`/`DELETE` commands without a result
  value return `204 No Content`; commands with a result value return `201
  Created` (with a `Location` header) when `CommandDefinition.CreatedRouteName`
  is set, otherwise `200`.
- Add optional `CreatedRouteName` to `CommandDefinition`.
- Migrate `UserErrors`, `TenantErrors`, `RoleErrors`, and the demo's
  `*Errors.cs` files to the typed factories so none stay in `ErrorType.Failure`
  by omission.

## Capabilities

### New Capabilities
- `error-handling`: typed `Error`/`ErrorType` taxonomy in the domain layer and
  its translation to HTTP `ProblemDetails` responses (status codes, success
  codes) in the API layer.

### Modified Capabilities
(none — no pre-existing specs in this repo)

## Impact

- **Affected code**: `Raftel.Domain.Abstractions.Error` (new `Type` property,
  `static readonly` fields, typed factories); new `ErrorType` enum; new
  `Raftel.Api.Server.AutoEndpoints.ErrorResults`; `CommandEndpointMapper`,
  `QueryEndpointMapper`, `CommandDefinition`; `UserErrors`, `TenantErrors`,
  `RoleErrors` and demo equivalents.
- **Affected tests**: existing functional tests in
  `tests/Raftel.Api.FunctionalTests` that assert `400` on business-rule
  failures must be updated to the correct status code; new unit tests for
  `Error` factories and `ErrorResults`, new functional tests per `ErrorType`.
- **Consumers**: any client relying on `Error.None`/`Error.NullValue`
  reassignment (unlikely, but now a compile error), any client checking
  `== 200` on command endpoints, any client parsing the old flat `Error` JSON
  body instead of `ProblemDetails` + `code` extension. Documented in
  `BREAKING_CHANGES.md`.
- **No dependency changes**; no new packages.
