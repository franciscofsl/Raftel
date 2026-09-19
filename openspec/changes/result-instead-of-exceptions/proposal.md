## Why

`Raftel.Application`'s own middleware pipeline violates the framework's declared rule ("`Result`/`Result<T>` pattern for errors; no exceptions for business-logic flow", `CLAUDE.md`): `ValidationMiddleware` throws `ValidationException` and `PermissionAuthorizationMiddleware` throws `UnauthorizedException` (via `ICurrentUser.EnsureHasPermission`). This gives the framework two competing error channels (`Result` for business failures, exceptions for validation/authorization), forces `ExceptionHandlingMiddleware` to special-case both, breaks out of the pipeline in a way that will force `catch`-based rollback once explicit transactions land, and — being a didactic framework — teaches consumers the wrong pattern by example. Now that typed `Error`/`ErrorType` → HTTP mapping (#125) and `CancellationToken` propagation (#126) are both in place, the pipeline can return `Result` end-to-end without losing HTTP-contract fidelity.

## What Changes

- Add `ValidationError : Error`, aggregating every failed `Validator<T>` rule into a single `Error` without losing per-field detail. Requires `Error` to stop being implicitly sealed (record inheritance).
- **BREAKING**: Rewrite `ValidationMiddleware<TRequest, TResponse>` to return a failed `Result`/`Result<T>` (built via a cached `ResultFactory`) instead of throwing `ValidationException`, and constrain it to `where TResponse : Result`.
- **BREAKING**: Add `ICurrentUser.HasPermission(string permission) : bool` (non-throwing). Rewrite `PermissionAuthorizationMiddleware<TRequest, TResponse>` to return a failed `Result` with `ErrorType.Forbidden` instead of throwing `UnauthorizedException`.
- **BREAKING**: Mark `ICurrentUser.EnsureHasPermission`, `ValidationException`, and `UnauthorizedException` `[Obsolete]` (removed in a following version). `ExceptionHandlingMiddleware` keeps its existing `catch` blocks for the deprecation window, for consumer code that still throws them directly.
- Add `AuthorizationOptions.IncludeMissingPermissionsInError` (default `false` in Release, `true` in Development) controlling whether the `Forbidden` error message enumerates the missing permissions.
- **BREAKING**: Extend `ErrorResults.ToProblem` to detect `ValidationError` and expand it into a `Dictionary<string, string[]>` `errors` extension on the `ProblemDetails` response, matching ASP.NET Core's own validation-error shape. Today a real pipeline validation failure produces `errors: [{code, message, type}, ...]` (a flat array of `Error`, via `ExceptionHandlingMiddleware`'s `ValidationException` catch); this change replaces that with `errors: { "<Field>": ["<message>", ...] }`. Requires validators' `Error.Code` to be prefixed with the field name (e.g. `"Email.Invalid"`) — a convention to document on `Validator<T>`; existing `Error.Code`s that don't follow it (e.g. demo app codes like `"CreatePirate.NameRequired"`) still work but bucket under their code's prefix rather than a real field name.
- Add an architecture test asserting no type in `Raftel.Application.Middlewares` throws `ValidationException` or `UnauthorizedException`.
- Status codes are unchanged (400 for validation, 403 for authorization). The 403 body shape is unchanged. The 400 validation body's `errors` shape changes from an array to a field-keyed dictionary (see above) — this is the one deliberate, intentional break in the otherwise-preserved HTTP contract.

## Capabilities

### New Capabilities
- `error-handling/result-pipeline`: framework-wide requirement that the command/query middleware pipeline communicates business, validation, and authorization failures exclusively through `Result`/`Result<T>`, never through thrown exceptions.

### Modified Capabilities
(none — no existing capability under `openspec/specs/` covers this behavior yet)

## Impact

- **Affected code**: `src/Raftel.Domain/Abstractions/Error.cs`, new `ValidationError.cs`; `src/Raftel.Application/Middlewares/{ValidationMiddleware,PermissionAuthorizationMiddleware}.cs`; new `src/Raftel.Application/Abstractions/ResultFactory.cs`; new `src/Raftel.Application/AuthorizationOptions.cs`; `src/Raftel.Application/Abstractions/Authentication/ICurrentUser.cs`; `src/Raftel.Infrastructure/Authentication/CurrentHttpUser.cs`; `src/Raftel.Application/Exceptions/{ValidationException,UnauthorizedException}.cs`; `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`; `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs`.
- **Public API**: breaking — `ICurrentUser` gains a member and obsoletes another; `ValidationMiddleware`/`PermissionAuthorizationMiddleware` change their failure signaling mechanism (still source-compatible for consumers who only depend on the `IGlobalMiddleware` contract and `Result`, not on catching the exceptions); the validation failure `errors` body shape changes from an array to a field-keyed dictionary. Tracked in `BREAKING_CHANGES.md`.
- **Tests**: unit tests for `ResultFactory`, `ValidationError`, both rewritten middlewares (happy/failure path, `next` not invoked on failure), `CurrentHttpUser.HasPermission`; functional regression tests asserting identical HTTP status/body shape pre- and post-change; architecture test forbidding the two exception types in `Middlewares`.
- **Dependencies**: none new. Depends on already-merged #125 (typed `Error`/HTTP mapping) and #126 (`CancellationToken` propagation).
