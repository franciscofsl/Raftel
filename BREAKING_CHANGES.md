# Breaking Changes - .NET 10 Migration

This document describes the breaking changes introduced during the migration from .NET 9 to .NET 10.

## Framework Update

### .NET 10 Target Framework
**What changed:** All projects now target .NET 10 instead of .NET 9.

**Why:** To adopt the latest framework version and benefit from new features, performance improvements, and security updates.

**Impact:** Consumers must upgrade to .NET 10 SDK to build and run the library.

**Action required:** 
- Install [.NET 10 SDK](https://dotnet.microsoft.com/download) (version 10.0 or higher)
- Update your project's `TargetFramework` to `net10.0` if you're consuming this library

---

## NuGet Package Updates

### Microsoft.OpenApi 2.0.0
**What changed:** Updated from Microsoft.OpenApi 1.6.x to 2.0.0

**Why:** Required for compatibility with .NET 10's `Microsoft.AspNetCore.OpenApi 10.0.1` package.

**Impact:** The `Microsoft.OpenApi.Models` namespace has been removed. All OpenApi types are now in the `Microsoft.OpenApi` namespace directly.

**Action required:**
If you're using the Raftel.Api.Server package and customizing OpenAPI configurations:
- Change `using Microsoft.OpenApi.Models;` to `using Microsoft.OpenApi;`
- `OpenApiSchema.Type` now uses `JsonSchemaType` enum instead of string
- Update custom OpenAPI filters and configurations according to [Microsoft.OpenApi 2.0 migration guide](https://github.com/dotnet/aspnetcore/issues/61123)

**Example:**
```csharp
// Before (.NET 9)
using Microsoft.OpenApi.Models;

var schema = new OpenApiSchema 
{ 
    Type = "string",
    Format = "uuid"
};

// After (.NET 10)
using Microsoft.OpenApi;

var schema = new OpenApiSchema 
{ 
    Type = JsonSchemaType.String,
    Format = "uuid"
};
```

### Swashbuckle.AspNetCore 10.1.0
**What changed:** Updated from Swashbuckle.AspNetCore 8.1.1 to 10.1.0

**Why:** Required for compatibility with Microsoft.OpenApi 2.0.0

**Impact:** If you're using Swashbuckle in your application that consumes this library, you may need to update custom Swagger filters and configurations.

**Action required:**
- Update Swashbuckle.AspNetCore to version 10.1.0 or higher
- Review and update custom Swagger filters according to [Swashbuckle v10 migration guide](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md)

### Entity Framework Core 10.0.1
**What changed:** Updated all Entity Framework Core packages from 9.0.4 to 10.0.1

**Why:** Required for .NET 10 compatibility

**Impact:** No breaking changes expected in normal usage.

**Action required:** Update your EF Core packages to 10.0.1 or higher if you're using Entity Framework.

### Microsoft.Extensions.* Packages 10.0.1
**What changed:** Updated all Microsoft.Extensions packages from 9.0.x to 10.0.1

**Why:** Required for .NET 10 compatibility

**Impact:** No breaking changes expected in normal usage.

**Action required:** Update your Microsoft.Extensions packages to 10.0.1 or higher.

---

## Deprecated APIs

### WithOpenApi Extension Method
**What changed:** The `WithOpenApi` extension method is now marked as obsolete in .NET 10.

**Why:** Microsoft is moving towards a new OpenAPI generation approach.

**Impact:** You'll see deprecation warnings (ASPDEPR002) when using `WithOpenApi`.

**Action required:** 
- The method still works in .NET 10 but will be removed in a future version
- Plan to migrate to the new OpenAPI generation APIs when they become available
- For now, you can suppress the warning or continue using it until a replacement is provided

---

---

## Typed Error/HTTP Status Mapping

### `Error.None` and `Error.NullValue` are now immutable
**What changed:** `Error.None` and `Error.NullValue` changed from mutable `public static` fields to `static readonly` fields.

**Why:** Both were reassignable by any code in the process, which was a latent bug — anything could silently change what "no error" or "null value" meant for the rest of the application's lifetime.

**Impact:** Code that reassigned `Error.None` or `Error.NullValue` (unlikely, but technically possible before) no longer compiles.

**Action required:** Remove any code that assigns to `Error.None` or `Error.NullValue`. There is no supported replacement — these values are meant to be fixed.

### Command endpoints no longer always return `200 OK`
**What changed:** Command endpoints (`POST`/`PUT`/`DELETE` mapped via `AddCommand`) now return status codes based on the outcome instead of a hardcoded `200 OK`:
- No result value on success → `204 No Content`.
- A result value on success, with `CreatedRouteName` configured on the command → `201 Created` with a `Location` header.
- A result value on success, with no `CreatedRouteName` configured → `200 OK` with the result body (unchanged).

**Why:** A blanket `200 OK` for every successful command ignored REST conventions and didn't let clients tell "resource created" from "no content to return" from "here's the updated resource."

**Impact:** Clients that check `response.StatusCode == 200` on a command endpoint that produces no result value, or that expect a body on such an endpoint, will break.

**Action required:** Update clients to accept `204` for no-result commands, and `201` (with `Location`) for commands whose `CommandDefinition` declares a `CreatedRouteName`.

### Failed results now return typed `ProblemDetails`, not a flat `Error` body with a hardcoded `400`
**What changed:** Command and query endpoints translate a failed `Result` to an HTTP response based on the error's `ErrorType`: `Validation`/`Failure` → 400, `NotFound` → 404, `Conflict` → 409, `Unauthorized` → 401, `Forbidden` → 403, `Unexpected` → 500. The response body is an RFC 7807 `ProblemDetails` document with the original `Error.Code` as a `code` extension field, instead of a plain serialized `Error`.

**Why:** Every business-rule failure previously returned `400 Bad Request` regardless of its actual nature (missing resource, conflict, permission denied, etc.), and used a different error shape than the RFC 7807 `ProblemDetails` already returned by unhandled-exception handling.

**Impact:** Clients that assume every failed command/query response is `400` with a flat `{ code, message }` body will break — status codes now vary by error type, and the body shape is `ProblemDetails` (`title`, `status`, `detail`, `code` extension) instead.

**Action required:** Update clients to branch on the actual HTTP status code and to read the `ProblemDetails` shape, using the `code` extension field for the machine-readable error code.

---

## CancellationToken Propagation

### `IGlobalMiddleware.HandleAsync` gains a `CancellationToken` parameter
**What changed:** `IGlobalMiddleware<TRequest, TResponse>.HandleAsync` now takes a third parameter, `CancellationToken cancellationToken`. This propagates to `ICommandMiddleware<T>`, `ICommandMiddleware<T,R>`, and `IQueryMiddleware<T,R>`, which inherit from `IGlobalMiddleware`.

**Why:** The token existed at the endpoint and at `IRequestHandler`/`IRepository`, but was dropped everywhere in between — no dispatched operation could actually be cancelled. An aborted HTTP request kept hitting the database until it finished.

**Impact:** Any custom middleware implementing `IGlobalMiddleware`/`ICommandMiddleware`/`IQueryMiddleware` no longer compiles.

**Action required:** Add the `CancellationToken cancellationToken` parameter to your `HandleAsync` implementation and forward it to `next(cancellationToken)`.

**Example:**
```csharp
// Before
public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    => next();

// After
public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
    => next(cancellationToken);
```

### `RequestHandlerDelegate<TResponse>` gains a `CancellationToken` parameter
**What changed:** `RequestHandlerDelegate<TResponse>` changed from `delegate Task<TResponse> RequestHandlerDelegate<TResponse>()` to `delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken)`.

**Why:** This is the `next` delegate middleware calls to continue the pipeline; it needed to carry the token forward alongside the change to `IGlobalMiddleware`.

**Impact:** Any code invoking `next()` with no arguments no longer compiles.

**Action required:** Call `next(cancellationToken)`, passing through the token your middleware received.

### `IRequestDispatcher.DispatchAsync` and `IQueryDispatcher.DispatchAsync` accept a `CancellationToken`
**What changed:** Both gain a `CancellationToken cancellationToken = default` parameter. `ICommandDispatcher.DispatchAsync` already declared the parameter but silently discarded it — it now actually propagates it.

**Why:** Without it, `IQueryDispatcher` had no way to pass a token to the pipeline at all, and `ICommandDispatcher` accepted one only to throw it away.

**Impact:** Custom `IRequestDispatcher`/`IQueryDispatcher` implementations no longer compile. Callers relying on the (previously no-op) `ICommandDispatcher` token now get real cancellation.

**Action required:** Add the parameter to custom implementations and forward it to the underlying pipeline call.

### `UnitOfWorkMiddleware` always commits with `CancellationToken.None`
**What changed:** `UnitOfWorkMiddleware` no longer forwards the request's `CancellationToken` to `IUnitOfWork.CommitAsync` — it always commits with `CancellationToken.None`.

**Why:** Cancelling mid-`SaveChanges` leaves the transaction in an indeterminate state and can abort in-flight domain event dispatch. Once a handler has returned a successful `Result`, the commit is deliberately not cancellable.

**Impact:** A request whose token is cancelled after its handler succeeds but before the commit will still persist its changes, rather than rolling back. This is intentional; see the type's XML doc.

**Action required:** None, unless you had (incorrectly) relied on late cancellation aborting an already-successful command's commit.

---

## Summary

The migration to .NET 10 is primarily focused on framework and dependency updates. The main breaking change that may affect consumers is the Microsoft.OpenApi 2.0 update, which requires namespace and type changes if you're customizing OpenAPI/Swagger configurations.

A separate, later change introduced a typed `Error`/`ErrorType` taxonomy with corresponding HTTP status code mapping, immutable well-known `Error` values, and REST-appropriate success codes for command endpoints — see the "Typed Error/HTTP Status Mapping" section above.

All tests pass successfully after migration, confirming that the functional behavior of the library remains unchanged except where documented above.
