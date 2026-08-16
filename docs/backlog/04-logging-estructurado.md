# 04 — Logging estructurado y correlation ID

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Api.Server`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [02](02-propagacion-cancellationtoken.md)

## 1. Motivación

**No hay una sola referencia a `ILogger` en todo `src/`.** El framework es
completamente mudo.

El caso más grave está en `ExceptionHandlingMiddleware`:

```csharp
catch (Exception)
{
    await HandleInternalServerErrorAsync(context);   // ← excepción tragada, sin registrar
}
```

Un `500` en producción no deja rastro: ni tipo de excepción, ni stack trace, ni
qué comando lo provocó. Diagnóstico imposible.

Además no hay forma de correlacionar la petición HTTP con los eventos que genera
(comando ejecutado, eventos de dominio despachados, auditoría escrita).

## 2. Diseño

### 2.1 Dependencia

`Raftel.Application` añade `Microsoft.Extensions.Logging.Abstractions` (solo
abstracciones, sin implementación — coherente con "sin dependencias externas":
`Microsoft.Extensions.*` ya se usa para DI).

### 2.2 `LoggingMiddleware`

Middleware global, **el primero** de la cadena para envolver a todos los demás.

```csharp
public sealed class LoggingMiddleware<TRequest, TResponse>(ILogger<LoggingMiddleware<TRequest, TResponse>> logger)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var timestamp = Stopwatch.GetTimestamp();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["RequestType"] = request is ICommand or ICommand<object> ? "Command" : "Query"
        });

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            if (response is Result { IsFailure: true } failure)
            {
                logger.LogWarning("{RequestName} failed with {ErrorCode}: {ErrorMessage} in {ElapsedMs}ms",
                    requestName, failure.Error.Code, failure.Error.Message, elapsed.TotalMilliseconds);
            }
            else
            {
                logger.LogInformation("{RequestName} handled in {ElapsedMs}ms", requestName, elapsed.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{RequestName} threw {ExceptionType}", requestName, exception.GetType().Name);
            throw;
        }
    }
}
```

Puntos de diseño:

- **Nunca se loguea el contenido del request.** Un `RegisterUserCommand` lleva
  contraseña, un `LogInUserQuery` también. Solo el nombre del tipo. Si en el
  futuro se quiere el payload, será opt-in por atributo `[LogPayload]` con lista
  de propiedades a redactar.
- `Result` fallido ⇒ `Warning`, no `Error`: es flujo de negocio esperado.
- Excepción ⇒ `Error` + rethrow. No cambia el flujo.
- Message templates con placeholders nombrados (logging estructurado real, no
  interpolación de strings).

### 2.3 Correlation ID

```csharp
// Raftel.Application/Abstractions/ICorrelationContext.cs
public interface ICorrelationContext
{
    string CorrelationId { get; }
}
```

Implementación en `Raftel.Infrastructure` sobre `IHttpContextAccessor`:

- Lee la cabecera `X-Correlation-Id` de la petición entrante.
- Si no existe, genera un `Guid` nuevo.
- La escribe en la respuesta como `X-Correlation-Id`.
- Se registra como scope de logging en el middleware ASP.NET, de modo que
  **todos** los eventos de la petición lo llevan.

Middleware ASP.NET `CorrelationIdMiddleware`, colocado antes de
`ExceptionHandlingMiddleware`.

> Sanear la cabecera entrante: máximo 128 caracteres, solo alfanuméricos, guion y
> guion bajo. Una cabecera arbitraria del cliente acaba en los logs; sin filtro
> es una vía de log injection / forging de líneas.

### 2.4 Corregir `ExceptionHandlingMiddleware`

```csharp
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // ...
    catch (Exception exception)
    {
        logger.LogError(exception,
            "Unhandled exception on {Method} {Path}",
            context.Request.Method, context.Request.Path);
        await HandleInternalServerErrorAsync(context);
    }
}
```

Se mantiene la garantía actual: **el stack trace nunca sale en la respuesta**,
solo al log.

`ValidationException` ⇒ `LogDebug`. `UnauthorizedException` ⇒ `LogWarning` con
el permiso requerido (útil para detectar escaladas de privilegio).

### 2.5 Registro

```csharp
services.AddRaftelApplication(builder =>
{
    builder.RegisterServicesFromAssembly(typeof(Program).Assembly);
    builder.AddGlobalMiddleware(typeof(LoggingMiddleware<,>));    // primero
    builder.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    // ...
});
```

El orden lo determina el consumidor (`RaftelApplicationBuilder` respeta el orden
de registro). Documentar que `LoggingMiddleware` va primero.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Middlewares/LoggingMiddleware.cs`
- `src/Raftel.Application/Abstractions/ICorrelationContext.cs`
- `src/Raftel.Infrastructure/Correlation/CorrelationContext.cs`
- `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddleware.cs`
- `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddlewareExtensions.cs`
- `tests/Raftel.Application.UnitTests/Middlewares/LoggingMiddlewareTests.cs`
- `tests/Raftel.Api.FunctionalTests/CorrelationIdTests.cs`

**Modificados**
- `src/Raftel.Application/Raftel.Application.csproj` — `Microsoft.Extensions.Logging.Abstractions`
- `Directory.Packages.props`
- `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `demo/**/Program.cs`

## 4. Plan de implementación

1. Añadir el paquete de abstracciones de logging.
2. **(test)** `LoggingMiddlewareTests` con un `ILogger` sustituido (NSubstitute):
   éxito ⇒ `Information`; `Result` fallido ⇒ `Warning` con el código de error;
   excepción ⇒ `Error` y la excepción se relanza.
3. **(test)** El middleware **no** registra ninguna propiedad del request
   (verificar que el payload no aparece en los argumentos del log).
4. Implementar `LoggingMiddleware`.
5. **(test)** `ExceptionHandlingMiddleware` registra en `Error` y sigue sin
   exponer stack trace en el cuerpo.
6. Corregir `ExceptionHandlingMiddleware`.
7. **(test funcional)** Petición con `X-Correlation-Id` conocido ⇒ misma
   cabecera en la respuesta. Sin cabecera ⇒ se genera una.
8. **(test)** Cabecera maliciosa (200 chars, saltos de línea) queda saneada.
9. Implementar `CorrelationIdMiddleware` y `CorrelationContext`.
10. Cablear en la demo, documentar el orden de middlewares.

## 5. Tests

- **Unit**: niveles de log por escenario, no filtrado de payload, rethrow.
- **Unit**: saneado de la cabecera de correlación.
- **Functional**: propagación de la cabecera ida y vuelta; un `500` provocado
  deja entrada de log (capturable con un `ILoggerProvider` de test).

## 6. Criterios de aceptación

- [ ] Ninguna excepción se traga sin registrar.
- [ ] El stack trace no aparece en ninguna respuesta HTTP.
- [ ] Ningún log contiene contraseñas ni tokens.
- [ ] Todos los eventos de una petición comparten `CorrelationId`.
- [ ] `X-Correlation-Id` presente en toda respuesta.

## 7. Alternativas descartadas

- **Serilog directo en el framework**: ata al consumidor a un sink concreto.
  Con `ILogger` el consumidor elige (Serilog, NLog, OpenTelemetry — ver
  [15](15-observabilidad-opentelemetry.md)).
- **Loguear el payload completo por defecto**: fuga de credenciales garantizada.
- **`Activity.Current.Id` como correlation ID sin cabecera propia**: válido, pero
  se cubre en la feature de OpenTelemetry; aquí interesa una cabecera explícita
  e independiente de que haya tracing configurado.
