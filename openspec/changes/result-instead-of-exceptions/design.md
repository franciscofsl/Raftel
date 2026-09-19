## Context

See proposal.md - Why. Two framework middlewares (`ValidationMiddleware`, `PermissionAuthorizationMiddleware` in `src/Raftel.Application/Middlewares/`) throw (`ValidationException`, `UnauthorizedException` in `src/Raftel.Application/Exceptions/`) instead of returning a failed `Result`, and `ExceptionHandlingMiddleware`/`ErrorResults` (`Raftel.Api.Server`) already map `Error`/`ErrorType` to HTTP status via #125's typed-error work. `TResponse` in the middleware pipeline is generic over `Result` and `Result<T>` (per use case), so constructing a typed failure without knowing `T` at compile time is the central technical problem this design solves.

## Goals / Non-Goals

**Goals:**
- Build a failed `Result` or `Result<T>` for an arbitrary closed generic `TResponse` without per-`T` boilerplate, and without a reflection cost that shows up per-request.
- Preserve the current HTTP status codes for both failure paths, and the current body shape for the authorization path. The validation failure body deliberately changes shape (array → field-keyed dictionary) — see proposal.md's breaking-changes list; this is the one intentional break.
- Keep `ValidationException`/`UnauthorizedException` usable during a deprecation window for consumer code that throws them directly (they aren't removed by this change, only obsoleted).

**Non-Goals:**
- Removing `ValidationException`/`UnauthorizedException` outright (scheduled for a later version per proposal.md).
- Changing anything about explicit transactions/rollback (tracked separately; this change only removes one of the reasons a `catch` would be needed there).
- Any change to `Error`/`ErrorType`/HTTP-mapping behavior itself (already delivered by #125).

## Decisions

### `ResultFactory`: cached compiled delegate per `TResponse`, not raw reflection per call
`ValidationMiddleware`/`PermissionAuthorizationMiddleware` know `TResponse` as a generic parameter but must produce `Result.Failure(error)` for `TResponse = Result` or `Result<T>.Failure(error)` for `TResponse = Result<T>`, without a `case` per possible `T`. A `ConcurrentDictionary<Type, Func<Error, object>>` builds one expression-tree-compiled delegate the first time a given `TResponse` is seen (via `Result.Failure` or `Result<T>.Failure` through reflection once, `Expression.Lambda(...).Compile()`), then reuses it on every subsequent request for that `TResponse`. This keeps steady-state cost to a dictionary lookup + delegate invoke, which is what justifies moving off exceptions for a per-request path in the first place (exceptions are also not free, but the old code paid that cost only on the failure path — the new code must not regress the success path, and must keep the failure path cheap too).
- **Alternative considered**: call `MethodInfo.Invoke` via reflection on every failure. Rejected — no caching means the reflection cost recurs each time, undermining the motivation (existing spec doc already flags this as "se nota").
- **Alternative considered**: make every command/query handler return a common non-generic `Result` (drop `Result<T>`). Rejected — loses typed return values for queries, a much larger and unrelated API change.

### `ValidationMiddleware<TRequest, TResponse>` gains `where TResponse : Result`
`IGlobalMiddleware<TRequest, TResponse>` itself stays unconstrained (some future middleware might operate over non-`Result` responses, e.g. streaming) but `ValidationMiddleware` and `PermissionAuthorizationMiddleware` specifically add `where TResponse : Result` since they only make sense for CQRS requests, which always return `Result`/`Result<T>`. This is a source-compatible constraint for existing registrations, since every `ICommand`/`ICommand<T>`/`IQuery<T>` already resolves to a `Result`-derived `TResponse`.

### `ValidationError` as a subtype of `Error`, not a new parallel error kind
Aggregating N validator failures into the single `Error` a `Result.Failure` expects requires either a new field on `Error` (`IReadOnlyList<Error>? Errors`) or a subtype. A subtype (`ValidationError : Error`) keeps `Error` itself simple for the common single-error case and matches the domain layer's existing pattern of typed errors. Requires unsealing `Error` (implicitly sealed today as a plain `record` with no `sealed` keyword — actually already unsealed; only inheritance-safety needs a value-equality test, since positional records generate `Equals` based on the declared type).
- **Alternative considered**: add `Errors` list directly to `Error`. Rejected — forces every `Error` consumer to handle a case that's only meaningful for validation.

### `ICurrentUser.HasPermission` added alongside, not replacing, `EnsureHasPermission`
Adding a non-throwing query method and obsoleting the throwing one (rather than changing `EnsureHasPermission`'s behavior) keeps existing direct callers of `EnsureHasPermission` compiling (with a warning) through the deprecation window, per proposal.md's breaking-change list.

### `AuthorizationOptions.IncludeMissingPermissionsInError` defaults to `false`, set by the consumer
Default `false` (safe by default, matching the spec's "Default configuration" scenario). `RaftelApplicationBuilder` gains `ConfigureAuthorization(Action<AuthorizationOptions> configure)` (mirroring `ConfigurePagination`) so a consumer can opt in, e.g. `builder.ConfigureAuthorization(o => o.IncludeMissingPermissionsInError = env.IsDevelopment())`. The framework itself does not read `IHostEnvironment` — `Raftel.Application` has no ASP.NET Core/Hosting dependency today (enforced by `Application_Should_Not_DependOn_AspNetCore` in `Raftel.ArchitectureTests`) and proposal.md commits to no new dependencies; auto-detecting Development is therefore the consuming application's responsibility, not the framework's.

### `ErrorResults.ToProblem` pattern-matches on `ValidationError` to shape the `errors` extension
Keeps the field-detail expansion (`Dictionary<string, string[]>`) local to the one place that already turns `Error` into `ProblemDetails`, rather than teaching `ValidationMiddleware` about HTTP. Requires validators' `Error.Code` to carry a `"<Field>.<Reason>"` convention so the field name can be split out; this is documented on `Validator<T>`, not enforced by a type.

## Risks / Trade-offs

- **[Risk]** Splitting a field name out of `Error.Code` by convention (`"Email.Invalid"`) is stringly-typed and can silently produce a malformed `errors` map if a validator doesn't follow the convention. → Mitigation: document the convention prominently on `Validator<T>` and cover it with a `ResultFactory`/`ValidationError` unit test asserting the split; a missing `.` falls back to putting the whole code under a single `""`-keyed bucket rather than throwing.
- **[Risk]** The expression-tree-compiled delegate cache is a new piece of reflection-adjacent infrastructure; a bug there fails every request going through validation or authorization, not just an edge case. → Mitigation: `ResultFactoryTests` covers `Result`, `Result<Guid>`, `Result<string>`, a record `T`, and a generic nested `T`, per proposal.md's test plan; functional regression tests assert the HTTP contract is unchanged end-to-end.
- **[Risk]** `where TResponse : Result` on the two middlewares is a compile-time breaking change for any consumer who registered them against a non-`Result` `TResponse` (unlikely, since `ICommand`/`IQuery<T>` already constrain this, but not impossible with a hand-rolled `IRequest<T>`). → Mitigation: called out explicitly in proposal.md's breaking-changes list and `BREAKING_CHANGES.md`.
- **[Trade-off]** Keeping `ValidationException`/`UnauthorizedException` `[Obsolete]` rather than deleting them means `ExceptionHandlingMiddleware` still carries two dead-ish `catch` blocks for this release. Accepted deliberately as the deprecation window proposal.md commits to.

## Migration Plan

1. Ship `ValidationError`, `ResultFactory`, `AuthorizationOptions`, and the rewritten middlewares together (they're only meaningful as a set).
2. Mark `ValidationException`, `UnauthorizedException`, and `ICurrentUser.EnsureHasPermission` `[Obsolete]` in the same release — no separate deprecation-only release, since nothing consumes the new members yet that would conflict.
3. Functional regression tests must pass unchanged (same status codes/body shape) before merge — this is the release gate, not a follow-up.
4. Document the breaking changes and the field-name-prefix validator convention in `BREAKING_CHANGES.md` and `Validator<T>`'s doc comments.
5. Removal of the obsoleted members is out of scope for this change and tracked for a later major version.

Rollback: revert the change wholesale (it's additive-plus-rewrite within a single release, no data migration involved); consumers who already adopted `HasPermission`/`ValidationError` would need to revert alongside.
