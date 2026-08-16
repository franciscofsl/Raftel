## Context

See `proposal.md` - Why. Constraints that shape this design:

- `Raftel.Domain` must not depend on HTTP concepts (project-wide layering
  rule; `Microsoft.EntityFrameworkCore` and ASP.NET types are already
  forbidden in `Domain`).
- `Error`/`Result` is the framework's declared error-handling pattern —
  exceptions for business-logic flow are out (see root `CLAUDE.md`).
- `Error` is currently a two-argument positional record consumed by
  `UserErrors`, `TenantErrors`, `RoleErrors`, and their demo equivalents; the
  extension must not force every call site to change in the same commit that
  introduces the type.
- `ExceptionHandlingMiddleware` already emits RFC 7807 `ProblemDetails` for
  thrown exceptions; the new `Result`-based path must converge on the same
  shape.

## Goals / Non-Goals

**Goals:**
- Give `Error` a semantic `ErrorType` with typed factories, defaulting
  existing two-arg construction to `Failure` so nothing breaks at compile
  time.
- Provide one translation point (`ErrorResults`) from `Error` to an HTTP
  `ProblemDetails` response, replacing the blanket `Results.BadRequest`.
- Fix success status codes for command endpoints (204/201/200) to match REST
  conventions.
- Migrate the framework's own `*Errors.cs` files to typed factories as proof
  the taxonomy is usable end-to-end.

**Non-Goals:**
- Replacing exceptions with `Result` inside the middleware pipeline
  (`ValidationException`, `UnauthorizedException` stay as-is; that's a
  separate, later backlog item).
- Localizing error messages.
- Changing the query endpoint success contract (stays `200`).
- Introducing per-field validation error detail shape — this change only
  fixes the top-level status/type mapping.

## Decisions

**`ErrorType` lives in `Raftel.Domain.Abstractions`, `ErrorResults` lives in
`Raftel.Api.Server`.** The enum is a pure classification with no transport
meaning (`NotFound`, not `Http404`); only the API layer knows what a status
code is. This keeps the layering rule intact and lets a future non-HTTP
transport (e.g. gRPC) map the same `ErrorType` differently.

*Alternative considered*: put an `int HttpStatusCode` directly on `Error`.
Rejected — leaks HTTP into the domain, and a single int can't express
different transports' status conventions.

**`Error` keeps its two-arg constructor and adds `Type` with a default
value**, rather than introducing a new type or requiring a breaking
constructor change. `Error.None` and `Error.NullValue` become `static
readonly` as part of this same change since they're touched anyway and the
mutability is a latent bug (anyone can currently reassign `Error.None`).

*Alternative considered*: typed exceptions per category (`NotFoundException`,
`ConflictException`). Rejected — contradicts the project's declared
`Result`-only error pattern for business flow.

*Alternative considered*: a `Dictionary<string, int>` mapping error codes to
status codes. Rejected — requires a global registry and couples
infrastructure to specific domain error codes instead of a stable, closed
taxonomy.

**Success codes are read from HTTP semantics, not from whether the command
"worked."** `CommandDefinition` gains an optional `CreatedRouteName`; its
presence is what triggers `201` + `Location`, not the mere presence of a
result value. No `CreatedRouteName` + a result value keeps returning `200`
(no incorrect `204` when there's a body to return).

**Migration of `UserErrors`/`TenantErrors`/`RoleErrors` happens in the same
change**, not deferred, so the acceptance criterion "no `*Errors.cs` left at
`Failure` by omission" is met and existing functional tests exercise the real
mapping rather than a synthetic one.

## Risks / Trade-offs

[Existing functional tests assert `400` on business-rule failures that will
now correctly return `404`/`409`/`403`] → Mitigation: full regression pass
over `tests/Raftel.Api.FunctionalTests` is a required task, not optional
cleanup; each changed assertion is a deliberate, reviewed diff.

[Consumers outside this repo may depend on the current `200`-always and flat
`Error` JSON body] → Mitigation: documented as breaking changes in
`BREAKING_CHANGES.md`; this is a pre-1.0 framework where breaking changes are
expected to be called out per the existing `BREAKING_CHANGES.md` convention.

[`ErrorType.Failure` as the default for the two-arg constructor means a
forgotten migration silently stays at `400`] → Mitigation: acceptance
criterion explicitly checks every framework and demo `*Errors.cs` for
unclassified entries; not just "compiles," but "categorized."

## Migration Plan

1. Add `ErrorType` and extend `Error` (additive, non-breaking on its own).
2. Add `ErrorResults` and wire it into both endpoint mappers.
3. Add `CreatedRouteName` and the new success-code logic.
4. Migrate framework `*Errors.cs` and demo equivalents to typed factories.
5. Update `Error.None`/`Error.NullValue` to `static readonly`.
6. Run full test suite; fix functional test assertions that expected the old
   `400`-always / `200`-always behavior.
7. Document breaking changes in `BREAKING_CHANGES.md`.

No data migration or runtime rollback concerns — this is a compile-time and
HTTP-contract change with no persisted state involved.
