# 01 — `Error` tipado y mapeo `Result` → status HTTP

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Api.Server`
> Rompe API pública: **sí** (ver §8)
> Depende de: nada

## 1. Motivación

Hoy `Error` es `record Error(string Code, string Message)` sin categoría
semántica, y los mappers de endpoint colapsan **cualquier** fallo a `400 Bad Request`:

```csharp
// src/Raftel.Api.Server/AutoEndpoints/CommandEndpointMapper.cs
return result.IsSuccess
    ? Results.Ok()
    : Results.BadRequest(result.Error);
```

Consecuencias:

- Un agregado no encontrado devuelve `400` en vez de `404`.
- Un conflicto de unicidad devuelve `400` en vez de `409`.
- El cliente no puede distinguir "petición mal formada" de "regla de negocio violada".
- El body de error no es `ProblemDetails` (RFC 7807), pero
  `ExceptionHandlingMiddleware` sí lo es → **dos formatos de error distintos** en la misma API.

Esta es la primera feature del backlog porque el resto (paginación, validación
por `Result`, caching) devuelve errores y necesita la taxonomía ya estable.

## 2. Diseño

### 2.1 `ErrorType`

Enum en `Raftel.Domain.Abstractions`. Semántico, **sin** referencias HTTP: el
dominio no conoce el transporte.

```csharp
public enum ErrorType
{
    Failure = 0,     // fallo genérico no clasificado
    Validation,      // datos de entrada inválidos
    NotFound,        // recurso inexistente
    Conflict,        // choque de estado (unicidad, concurrencia)
    Unauthorized,    // sin autenticar
    Forbidden,       // autenticado pero sin permiso
    Unexpected       // fallo no controlado
}
```

### 2.2 `Error` extendido

`Error` pasa a llevar `Type` y expone factorías por categoría. Se conserva el
constructor posicional de dos parámetros para no romper todos los `*Errors.cs`
existentes (`UserErrors`, `TenantErrors`, `RoleErrors`), que quedarían en
`ErrorType.Failure` hasta migrarse.

```csharp
public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided", ErrorType.Validation);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message)   => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message)   => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message)  => new(code, message, ErrorType.Forbidden);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Failure(string code, string message)    => new(code, message, ErrorType.Failure);
}
```

> Nota: hoy `Error.None` y `Error.NullValue` son **campos mutables**
> (`public static Error None = ...`). Pasan a `static readonly` en el mismo
> cambio; es un bug latente (cualquiera puede reasignar `Error.None`).

### 2.3 `ErrorResults` — traducción a HTTP

Vive en `Raftel.Api.Server`, no en el dominio. Un único punto de traducción,
salida siempre `ProblemDetails`.

```csharp
namespace Raftel.Api.Server.AutoEndpoints;

internal static class ErrorResults
{
    public static IResult ToProblem(Error error) =>
        Results.Problem(
            title: TitleFor(error.Type),
            detail: error.Message,
            statusCode: StatusCodeFor(error.Type),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation   => StatusCodes.Status400BadRequest,
        ErrorType.NotFound     => StatusCodes.Status404NotFound,
        ErrorType.Conflict     => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected   => StatusCodes.Status500InternalServerError,
        _                      => StatusCodes.Status400BadRequest
    };

    private static string TitleFor(ErrorType type) => type switch { ... };
}
```

### 2.4 Códigos de éxito

Aprovechar el cambio para afinar los éxitos, hoy siempre `200 OK`:

| Caso | Hoy | Propuesto |
|---|---|---|
| `POST` command sin resultado | `200` | `204 No Content` |
| `POST` command con resultado | `200` | `201 Created` + header `Location` si `CommandDefinition` declara `CreatedRoute` |
| `PUT` command sin resultado | `200` | `204 No Content` |
| `DELETE` command | `200` | `204 No Content` |
| Query | `200` | `200` (sin cambio) |

`201 Created` requiere un campo opcional nuevo en `CommandDefinition`:

```csharp
public sealed record CommandDefinition
{
    // ...existente
    public string? CreatedRouteName { get; init; }  // null ⇒ 200/204
}
```

Si es `null`, comportamiento `204`. Sin ruta declarada no se inventa `Location`.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Abstractions/ErrorType.cs`
- `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs`
- `tests/Raftel.Domain.Tests/Abstractions/ErrorTests.cs`
- `tests/Raftel.Api.FunctionalTests/ErrorMappingTests.cs`

**Modificados**
- `src/Raftel.Domain/Abstractions/Error.cs` — `Type` + factorías + `static readonly`
- `src/Raftel.Api.Server/AutoEndpoints/CommandEndpointMapper.cs` — usar `ErrorResults`, códigos de éxito
- `src/Raftel.Api.Server/AutoEndpoints/QueryEndpointMapper.cs` — ídem
- `src/Raftel.Api.Server/AutoEndpoints/CommandDefinition.cs` — `CreatedRouteName`
- `src/Raftel.Domain/Features/Users/UserErrors.cs` — categorizar
- `src/Raftel.Domain/Features/Tenants/TenantErrors.cs` — categorizar
- `src/Raftel.Domain/Features/Authorization/RoleErrors.cs` — categorizar
- `demo/Raftel.Demo.Domain/**/*Errors.cs` — categorizar
- `BREAKING_CHANGES.md`

## 4. Plan de implementación

1. **(test)** `ErrorTests`: cada factoría produce el `ErrorType` correcto;
   `Error.None` sigue siendo igual por valor al construido con dos argumentos.
2. Añadir `ErrorType` y extender `Error`. Compilar toda la solución: no debe
   romper nada gracias al parámetro con valor por defecto.
3. **(test)** `ErrorMappingTests` funcional: un handler que devuelve
   `Error.NotFound(...)` responde `404` con `ProblemDetails` y extensión `code`.
4. Implementar `ErrorResults` y cablearlo en ambos mappers.
5. **(test)** Códigos de éxito: `POST` sin resultado ⇒ `204`; con
   `CreatedRouteName` ⇒ `201` + `Location`.
6. Implementar `CreatedRouteName` y los códigos de éxito.
7. Migrar `UserErrors`, `TenantErrors`, `RoleErrors` y los de la demo a las
   factorías tipadas. Ajustar los tests funcionales existentes que esperaban `400`.
8. Documentar en `BREAKING_CHANGES.md` y en `docs/`.

## 5. Tests

- **Unit (Domain)**: factorías, igualdad por valor, inmutabilidad de `None`.
- **Unit (Api.Server)**: `ErrorResults.ToProblem` por cada `ErrorType`.
- **Functional**: recorrido HTTP completo por cada categoría, verificando
  `Content-Type: application/problem+json`, status y cuerpo.
- **Regresión**: revisar todos los `Assert`/`ShouldBe` de
  `tests/Raftel.Api.FunctionalTests` que fijaban `400`.

## 6. Criterios de aceptación

- [ ] Todo fallo de API sale como `ProblemDetails`, tanto por `Result` como por excepción.
- [ ] `NotFound` ⇒ 404, `Conflict` ⇒ 409, `Forbidden` ⇒ 403, `Validation` ⇒ 400.
- [ ] `Error.None` y `Error.NullValue` son `static readonly`.
- [ ] Ningún `*Errors.cs` del framework ni de la demo queda en `ErrorType.Failure` por omisión.
- [ ] `dotnet test` en verde.

## 7. Alternativas descartadas

- **Excepciones tipadas por categoría** (`NotFoundException`, etc.): contradice
  "Result/Result<T> pattern for errors; no exceptions for business-logic flow"
  del `CLAUDE.md` raíz.
- **Mapa `Dictionary<string, int>` por código de error**: obliga a mantener un
  registro global y acopla infraestructura a códigos de dominio concretos.
- **`Error` con `int HttpStatusCode`**: mete HTTP en `Raftel.Domain`, viola la
  regla de dependencias.

## 8. Breaking changes

1. `Error.None` / `Error.NullValue` dejan de ser reasignables.
2. Endpoints de comando devuelven `204`/`201` donde antes devolvían `200`.
   Clientes que comprobaran `== 200` deben aceptar `2xx`.
3. El cuerpo de error pasa de `Error` serializado plano a `ProblemDetails`. El
   código sigue accesible en la extensión `code`.
