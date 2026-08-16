# 06 — `Result` en lugar de excepciones en el pipeline

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Api.Server`
> Rompe API pública: **sí**
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [02](02-propagacion-cancellationtoken.md)

## 1. Motivación

El `CLAUDE.md` raíz es explícito:

> **Result/Result<T>** pattern for errors; no exceptions for business-logic flow.

Pero el propio framework lo incumple en dos middlewares:

```csharp
// ValidationMiddleware
if (allErrors.Any()) throw new ValidationException(allErrors);

// PermissionAuthorizationMiddleware
currentUser.EnsureHasPermission(permission);   // lanza UnauthorizedException
```

Problemas concretos:

1. **Coste**: lanzar una excepción para un formulario mal rellenado es el camino
   caro para el caso frecuente.
2. **Doble camino de error**: los fallos de negocio salen por `Result` y los de
   validación/permiso por excepción, y `ExceptionHandlingMiddleware` los formatea
   por separado. Dos formatos, dos sitios donde tocar.
3. **Rompe el pipeline**: una excepción salta por encima de los middlewares
   externos. Con [05](05-transacciones-explicitas.md), obliga a `catch` para
   hacer rollback correcto.
4. **Predicar con el ejemplo**: es un framework didáctico; el consumidor copia
   los patrones que ve.

## 2. Diseño

### 2.1 `ValidationError`

Agregar múltiples errores en un solo `Error` sin perder detalle:

```csharp
// Raftel.Domain/Abstractions/ValidationError.cs
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("Validation.General", "One or more validation errors occurred.", ErrorType.Validation)
{
    public static ValidationError FromResults(IEnumerable<Result> results) => ...
}
```

Requiere que `Error` deje de ser `sealed` (hoy no lo es) y que la herencia de
records posicionales se mantenga bien con la igualdad por valor: añadir test.

### 2.2 `ValidationMiddleware` sin excepción

```csharp
public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
{
    var errors = validators
        .Select(validator => validator.Validate(request))
        .SelectMany(validationResult => validationResult.Errors)
        .ToList();

    if (errors.Count == 0)
    {
        return await next(cancellationToken);
    }

    return CreateValidationResult<TResponse>(new ValidationError(errors));
}
```

El punto delicado: `TResponse` es genérico (`Result` o `Result<T>`) y hay que
construir el fallo sin conocer `T` en tiempo de compilación.

```csharp
private static TResponse CreateValidationResult<TResponse>(ValidationError error)
{
    if (typeof(TResponse) == typeof(Result))
    {
        return (TResponse)(object)Result.Failure(error);
    }

    // Result<TValue>
    var valueType = typeof(TResponse).GetGenericArguments()[0];
    var failureMethod = typeof(Result)
        .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
        .MakeGenericMethod(valueType);

    return (TResponse)failureMethod.Invoke(null, [error])!;
}
```

**Cachear el `MethodInfo` por tipo** en un `ConcurrentDictionary<Type, Func<Error, object>>`
compilado con expression trees: el middleware corre en cada petición y la
reflexión sin cachear se nota.

Restricción a añadir en el middleware: `where TResponse : Result`. Hoy
`IGlobalMiddleware` no la impone; `ValidationMiddleware` sí puede hacerlo porque
todo `ICommand`/`IQuery` devuelve `Result` o `Result<T>`.

### 2.3 `PermissionAuthorizationMiddleware` sin excepción

`ICurrentUser` gana un método que no lanza:

```csharp
public interface ICurrentUser
{
    // existente
    void EnsureHasPermission(string permission);   // [Obsolete] → eliminar

    // nuevo
    bool HasPermission(string permission);
}
```

```csharp
var missing = requiredPermissions.Where(p => !currentUser.HasPermission(p)).ToArray();

if (missing.Length > 0)
{
    return CreateFailure<TResponse>(
        Error.Forbidden("Authorization.Forbidden", $"Missing permission: {string.Join(", ", missing)}"));
}

return await next(cancellationToken);
```

> Decisión de seguridad: el mensaje enumera los permisos que faltan. Es útil en
> desarrollo y en APIs internas, pero filtra el modelo de permisos a un atacante.
> Hacerlo configurable con `AuthorizationOptions.IncludeMissingPermissionsInError`,
> **por defecto `false`** en Release y `true` en Development.

### 2.4 Retirada de las excepciones

`ValidationException` y `UnauthorizedException` se marcan `[Obsolete]` en esta
versión y se eliminan en la siguiente. `ExceptionHandlingMiddleware` conserva sus
`catch` durante el periodo de deprecación (código de consumidor puede lanzarlas)
y luego queda solo con el `catch (Exception)` genérico.

Con el mapeo de [01](01-error-tipado-y-mapeo-http.md) ya en su sitio,
`ErrorType.Validation` ⇒ 400 y `ErrorType.Forbidden` ⇒ 403 se obtienen sin
código nuevo: el comportamiento HTTP observable **no cambia**.

### 2.5 Detalle por campo en `ProblemDetails`

`ErrorResults.ToProblem` detecta `ValidationError` y expande la extensión
`errors` como `Dictionary<string, string[]>` (formato que ya entienden los
clientes de ASP.NET Core):

```json
{
  "type": "...", "title": "Validation Error", "status": 400,
  "code": "Validation.General",
  "errors": { "Email": ["Email is not valid."], "Name": ["Name is required."] }
}
```

Esto exige que `Error.Code` de los validadores lleve el nombre del campo como
prefijo (`"Email.Invalid"`). Convención a documentar en `Validator<T>`.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Abstractions/ValidationError.cs`
- `src/Raftel.Application/AuthorizationOptions.cs`
- `src/Raftel.Application/Abstractions/ResultFactory.cs` (fábrica cacheada §2.2)
- `tests/Raftel.Application.UnitTests/Abstractions/ResultFactoryTests.cs`

**Modificados**
- `src/Raftel.Domain/Abstractions/Error.cs` — permitir herencia
- `src/Raftel.Application/Middlewares/ValidationMiddleware.cs`
- `src/Raftel.Application/Middlewares/PermissionAuthorizationMiddleware.cs`
- `src/Raftel.Application/Abstractions/Authentication/ICurrentUser.cs`
- `src/Raftel.Infrastructure/Authentication/CurrentHttpUser.cs`
- `src/Raftel.Application/Exceptions/{Validation,Unauthorized}Exception.cs` — `[Obsolete]`
- `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`
- `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs`
- `tests/**` — los que hoy hacen `Should.Throw<ValidationException>()`
- `BREAKING_CHANGES.md`

## 4. Plan de implementación

1. **(test)** `ResultFactory` construye `Result.Failure` y `Result<T>.Failure`
   para `T` = `Guid`, `string`, un record y un tipo genérico anidado.
2. Implementar `ResultFactory` con caché de delegados compilados.
3. **(test)** `ValidationError` agrega N errores y conserva igualdad por valor.
4. Implementar `ValidationError`.
5. **(test)** `ValidationMiddleware` devuelve `Result` fallido (no lanza) y **no**
   invoca `next`.
6. Reescribir `ValidationMiddleware`.
7. **(test)** `HasPermission` en `CurrentHttpUser` no lanza y devuelve `false`
   para usuario anónimo.
8. Añadir `HasPermission`, reescribir `PermissionAuthorizationMiddleware`.
9. **(test)** Con `IncludeMissingPermissionsInError = false`, el mensaje no
   enumera permisos.
10. **(test funcional)** Contrato HTTP idéntico al anterior: 400 con `errors`,
    403 sin cuerpo revelador.
11. Marcar excepciones obsoletas, actualizar tests, documentar.

## 5. Tests

- **Unit**: fábrica de resultados (el punto de mayor riesgo de la feature).
- **Unit**: ambos middlewares, camino feliz y fallido, verificando que `next`
  no se llama cuando hay fallo.
- **Functional**: **regresión de contrato** — mismos status y misma forma de
  cuerpo que antes del cambio.
- **Benchmark opcional**: comparar el coste del camino de validación fallida
  antes/después (justifica la feature con números).

## 6. Criterios de aceptación

- [ ] Ningún middleware del framework lanza excepciones para flujo de negocio.
- [ ] El contrato HTTP no cambia respecto a la versión anterior.
- [ ] La construcción de `Result<T>` por reflexión está cacheada.
- [ ] Los mensajes de permiso no filtran el modelo de autorización en Release.
- [ ] Test de arquitectura: ningún tipo en `Raftel.Application.Middlewares`
      lanza `ValidationException` ni `UnauthorizedException`.

## 7. Alternativas descartadas

- **Mantener las excepciones**: incumple la regla declarada del proyecto.
- **Hacer que todo devuelva `Result` sin genéricos** (un `Result` no tipado):
  pierde el valor de retorno de las queries.
- **`IGlobalMiddleware` con restricción `where TResponse : Result` global**:
  cerraría la puerta a middlewares sobre requests que no devuelvan `Result`,
  útiles a futuro (p. ej. streaming).

## 8. Breaking changes

1. `ICurrentUser.EnsureHasPermission` queda obsoleto (eliminación en la siguiente versión).
2. `ValidationException` y `UnauthorizedException` obsoletas.
3. Consumidores que capturaban `ValidationException` deben pasar a inspeccionar
   el `Result`.
