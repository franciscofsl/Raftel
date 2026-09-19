## 1. `ResultFactory`

- [x] 1.1 (test) `ResultFactoryTests`: building a failed `Result` for `TResponse = Result`
- [x] 1.2 (test) `ResultFactoryTests`: building a failed `Result<T>` for `T` = `Guid`, `string`, a record, and a generic nested type
- [x] 1.3 (test) `ResultFactoryTests`: the same `TResponse` reuses a cached delegate on a second call (no repeated reflection lookup)
- [x] 1.4 Implement `src/Raftel.Application/Abstractions/ResultFactory.cs`: `ConcurrentDictionary<Type, Func<Error, object>>` cache, expression-tree-compiled delegate per `TResponse`

## 2. `ValidationError`

- [x] 2.1 (test) `ValidationError` aggregates N `Error`s and exposes them without losing any
- [x] 2.2 (test) `ValidationError` value-equality works correctly through record inheritance from `Error`
- [x] 2.3 Implement `src/Raftel.Domain/Abstractions/ValidationError.cs` (`ValidationError(IReadOnlyList<Error> Errors) : Error`, `FromErrors` factory)
- [x] 2.4 Confirm `Error` supports inheritance (verify it is not `sealed`; add a regression test if needed)

## 3. `ValidationMiddleware` without exceptions

- [x] 3.1 (test) `ValidationMiddleware` returns a failed `Result`/`Result<T>` and does not invoke `next` when validators report errors
- [x] 3.2 (test) `ValidationMiddleware` invokes `next` and returns its result unchanged when validation passes
- [x] 3.3 (test) The failed `Result`'s error is a `ValidationError` aggregating every failed rule
- [x] 3.4 Rewrite `src/Raftel.Application/Middlewares/ValidationMiddleware.cs` to use `ResultFactory` + `ValidationError`, add `where TResponse : Result`, remove the `throw`
- [x] 3.5 Document the `"<Field>.<Reason>"` `Error.Code` convention on `Validator<T>`

## 4. `PermissionAuthorizationMiddleware` without exceptions

- [x] 4.1 (test) `CurrentHttpUser.HasPermission` returns `false` for an anonymous user without throwing
- [x] 4.2 (test) `CurrentHttpUser.HasPermission` returns `true`/`false` correctly for an authenticated user with/without the permission
- [x] 4.3 Add `HasPermission(string permission) : bool` to `src/Raftel.Application/Abstractions/Authentication/ICurrentUser.cs`; implement in `src/Raftel.Infrastructure/Authentication/CurrentHttpUser.cs`
- [x] 4.4 (test) `PermissionAuthorizationMiddleware` returns a failed `Result` with `ErrorType.Forbidden` and does not invoke `next` when a required permission is missing
- [x] 4.5 (test) `PermissionAuthorizationMiddleware` invokes `next` and returns its result unchanged when every required permission is present, and when the request has no `[RequiresPermission]` attribute
- [x] 4.6 Rewrite `src/Raftel.Application/Middlewares/PermissionAuthorizationMiddleware.cs` to use `HasPermission` + `ResultFactory`, add `where TResponse : Result`, remove the `throw`

## 5. Configurable permission-detail disclosure

- [x] 5.1 (test) With `IncludeMissingPermissionsInError = false`, the `Forbidden` error message does not list the missing permissions
- [x] 5.2 (test) With `IncludeMissingPermissionsInError = true`, the `Forbidden` error message lists the missing permissions
- [x] 5.3 Implement `src/Raftel.Application/AuthorizationOptions.cs` (default `false`); add `RaftelApplicationBuilder.ConfigureAuthorization(Action<AuthorizationOptions>)` so a consumer can opt in (e.g. `IsDevelopment()`), without the framework itself depending on hosting/environment APIs

## 6. Deprecate the exception-based path

- [x] 6.1 Mark `ICurrentUser.EnsureHasPermission` `[Obsolete]`
- [x] 6.2 Mark `src/Raftel.Application/Exceptions/ValidationException.cs` and `UnauthorizedException.cs` `[Obsolete]`
- [x] 6.3 Confirm `ExceptionHandlingMiddleware` keeps its existing `catch (ValidationException)` / `catch (UnauthorizedException)` blocks unchanged for the deprecation window

## 7. `ProblemDetails` field-level validation detail

- [x] 7.1 (test) `ErrorResults.ToProblem` expands a `ValidationError` into an `errors: Dictionary<string, string[]>` extension keyed by field name
- [x] 7.2 (test) A validator `Error.Code` without a `.` falls back to a single `""`-keyed bucket rather than throwing
- [x] 7.3 Implement the `ValidationError` branch in `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs`

## 8. Architecture and contract regression tests

- [x] 8.1 (test) Architecture test: no type in `Raftel.Application.Middlewares` throws `ValidationException` or `UnauthorizedException`
- [x] 8.2 (test, functional) A request failing validation over HTTP returns 400 with the new field-keyed `errors` body shape
- [x] 8.3 (test, functional) A request failing authorization over HTTP still returns 403 with no permission-revealing body by default

## 9. Documentation

- [x] 9.1 Add the breaking changes (`ICurrentUser.EnsureHasPermission` obsolete, `ValidationException`/`UnauthorizedException` obsolete, consumers must inspect `Result` instead of catching, validation-failure `errors` body shape changes from array to field-keyed dictionary) to `BREAKING_CHANGES.md`
- [x] 9.2 Update any `CLAUDE.md`/doc comments in `Raftel.Application`/`Raftel.Domain` that still describe validation or authorization failures as exception-based
