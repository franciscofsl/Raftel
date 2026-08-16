# 15 — Observabilidad con OpenTelemetry

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`, `Raftel.Api.Server`
> Rompe API pública: no (aditivo, opt-in)
> Depende de: [04](04-logging-estructurado.md), [09](09-health-checks.md)

## 1. Motivación

Con [04](04-logging-estructurado.md) hay logs, pero siguen faltando las otras dos
patas: **trazas** y **métricas**. Preguntas que hoy no tienen respuesta:

- ¿Cuánto tarda `CreateUserCommand` en el percentil 95?
- De los 800 ms de una petición, ¿cuánto es base de datos y cuánto handler?
- ¿Cuántos comandos fallan por validación vs. por permisos?
- ¿Cuánto lag acumula el outbox?

El pipeline de middlewares vuelve a ser el sitio ideal: instrumentando una vez,
se instrumentan todos los comandos y queries del consumidor.

## 2. Diseño

Solo `System.Diagnostics` (`ActivitySource`, `Meter`) en `src/`. **No** se
referencia el SDK de OpenTelemetry desde el framework: `ActivitySource` y `Meter`
son API del BCL, y el consumidor decide si exporta a OTLP, Prometheus,
Application Insights o nada.

### 2.1 Trazas

```csharp
// Raftel.Application/Diagnostics/RaftelDiagnostics.cs
public static class RaftelDiagnostics
{
    public const string ActivitySourceName = "Raftel";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
}
```

`TracingMiddleware` global, justo por dentro de `LoggingMiddleware`:

```csharp
using var activity = RaftelDiagnostics.ActivitySource.StartActivity(
    $"{requestType}/{requestName}", ActivityKind.Internal);

activity?.SetTag("raftel.request.name", requestName);
activity?.SetTag("raftel.request.type", requestType);      // Command | Query
activity?.SetTag("raftel.tenant.id", currentTenant.Id?.ToString());

var response = await next(cancellationToken);

if (response is Result { IsFailure: true } failure)
{
    activity?.SetStatus(ActivityStatusCode.Error, failure.Error.Code);
    activity?.SetTag("raftel.error.code", failure.Error.Code);
    activity?.SetTag("raftel.error.type", failure.Error.Type.ToString());
}
```

Reglas de atributos:

- **Nunca** el payload del request, por lo mismo que en logging.
- `Error.Code` sí (es un identificador estable); `Error.Message` no
  necesariamente (puede llevar datos del usuario).
- IDs de usuario y tenant: son datos personales en algunos marcos regulatorios.
  Opción `IncludeUserId`, por defecto `false`; `tenant.id` sí por defecto (es
  operativo, no personal).

### 2.2 Propagación de contexto

El `traceparent` de W3C lo maneja `Activity` automáticamente en HTTP entrante y
saliente. Los dos puntos donde hay que propagarlo a mano:

- **Outbox** ([07](07-outbox-domain-events.md)): guardar `TraceId`/`SpanId` en el
  `OutboxMessage` y restaurarlos como `ActivityContext` padre al procesarlo. Sin
  esto, el side-effect aparece como traza huérfana desconectada del comando.
- **Background jobs** ([16](16-background-jobs.md)): cada ejecución abre su
  propia traza raíz.

`CorrelationId` de [04](04-logging-estructurado.md) y `TraceId` conviven: se
añade el correlation ID como tag de la actividad para poder saltar entre logs y
trazas.

### 2.3 Métricas

```csharp
public static readonly Meter Meter = new("Raftel", Version);
```

| Instrumento | Tipo | Tags |
|---|---|---|
| `raftel.requests.count` | Counter | `request_name`, `request_type`, `outcome` (success/failure/exception) |
| `raftel.requests.duration` | Histogram (ms) | `request_name`, `request_type` |
| `raftel.requests.active` | UpDownCounter | `request_type` |
| `raftel.validation.failures` | Counter | `request_name` |
| `raftel.authorization.denied` | Counter | `permission` |
| `raftel.outbox.pending` | ObservableGauge | — |
| `raftel.outbox.dead_letter` | ObservableGauge | — |
| `raftel.cache.hits` / `.misses` | Counter | `query_name` |

> **Cardinalidad**: `request_name` es acotado (número de comandos del sistema),
> seguro como tag. Prohibido usar como tag cualquier ID de entidad, de usuario o
> de tenant en métricas: multiplica series y revienta el backend. Documentarlo.

### 2.4 Instrumentación de infraestructura

EF Core, ASP.NET Core y `HttpClient` ya emiten `ActivitySource` propios. El
framework solo documenta cómo registrarlos:

```csharp
// en la app del consumidor, no en el framework
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource(RaftelDiagnostics.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(RaftelDiagnostics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

Se ofrece un helper opcional en un paquete aparte
`Raftel.Infrastructure.OpenTelemetry` para no meter el SDK en el core.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Diagnostics/RaftelDiagnostics.cs`
- `src/Raftel.Application/Diagnostics/RaftelMetrics.cs`
- `src/Raftel.Application/Middlewares/TracingMiddleware.cs`
- `src/Raftel.Application/Middlewares/MetricsMiddleware.cs`
- `src/Raftel.Application/DiagnosticsOptions.cs`
- `tests/Raftel.Application.UnitTests/Diagnostics/**`

**Modificados**
- `src/Raftel.Infrastructure/Outbox/*` — propagar contexto de traza
- `src/Raftel.Infrastructure/Caching/*` — contadores de hit/miss
- `demo/**/Program.cs` + `docs/`

## 4. Plan de implementación

1. **(test)** Con un `ActivityListener` de prueba, ejecutar un comando produce una
   actividad con nombre y tags esperados.
2. Implementar `RaftelDiagnostics` y `TracingMiddleware`.
3. **(test)** Un `Result` fallido marca la actividad como `Error` con el código.
4. **(test)** El payload del request no aparece en ningún tag.
5. **(test)** Con `MeterListener`, un comando incrementa contador y registra duración.
6. Implementar `RaftelMetrics` y `MetricsMiddleware`.
7. Instrumentar caché y outbox.
8. **(test)** Traza del outbox enlazada como hija de la del comando original.
9. Paquete opcional con los helpers del SDK + ejemplo en la demo.
10. Documentar la política de cardinalidad y de datos personales.

## 5. Tests

- **Unit**: `ActivityListener` y `MeterListener` (permiten testear observabilidad
  sin exportador).
- **Unit**: ausencia de payload y de IDs personales con la configuración por defecto.
- **Integration**: correlación traza-comando-outbox.

## 6. Criterios de aceptación

- [ ] `src/` no referencia el SDK de OpenTelemetry, solo `System.Diagnostics`.
- [ ] Cada comando y query genera una actividad con duración y resultado.
- [ ] Ningún tag lleva payload ni, por defecto, ID de usuario.
- [ ] Las trazas del outbox enlazan con el comando que las originó.
- [ ] Documentada la política de cardinalidad de métricas.

## 7. Alternativas descartadas

- **Referenciar el SDK de OpenTelemetry desde `Raftel.Application`**: ata al
  consumidor a una versión concreta del SDK y contradice la política de mínimas
  dependencias. `ActivitySource`/`Meter` son BCL y las consume cualquier backend.
- **Métricas propias con `IMetricsCollector` a medida**: reinventar un estándar
  ya resuelto y perder la integración con todo el ecosistema.
- **Instrumentar en cada handler a mano**: repetitivo y se olvida; el pipeline
  lo hace una vez para todos.
