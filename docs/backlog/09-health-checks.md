# 09 — Health checks

> Estado: **Pendiente**
> Ámbito: `Raftel.Infrastructure`, `Raftel.Api.Server`
> Rompe API pública: no (aditivo)
> Depende de: [04](04-logging-estructurado.md)

## 1. Motivación

Cero health checks en la solución. Consecuencias operativas:

- Kubernetes / App Service no pueden decidir si reiniciar o retirar del balanceo
  una instancia. Sin `readiness`, el tráfico entra antes de que las migraciones
  terminen.
- No hay forma automática de detectar que la base de datos se ha caído; se
  descubre por errores 500 de usuario.
- Con [07](07-outbox-domain-events.md), un `OutboxProcessor` atascado es
  invisible hasta que alguien nota que no llegan emails.

## 2. Diseño

Sobre `Microsoft.Extensions.Diagnostics.HealthChecks`, incluido en el framework
compartido de ASP.NET Core (sin dependencia externa nueva).

### 2.1 Endpoints

Tres endpoints con propósitos distintos — la distinción importa:

| Endpoint | Tags | Significado | Uso |
|---|---|---|---|
| `/health/live` | ninguno | El proceso responde | `livenessProbe`. Fallo ⇒ reiniciar |
| `/health/ready` | `ready` | Dependencias listas | `readinessProbe`. Fallo ⇒ sacar del balanceo |
| `/health` | todos | Diagnóstico completo | Humanos / monitorización |

`/health/live` **no** debe tocar la base de datos: si la base cae, el pod no debe
reiniciarse en bucle — debe salir del balanceo, que es lo que hace `ready`.

### 2.2 Checks incluidos

```csharp
services.AddRaftelHealthChecks(options => { ... });
```

- **`DatabaseHealthCheck`** (`ready`): `dbContext.Database.CanConnectAsync()`
  con timeout corto. Degraded si tarda más de `SlowThreshold`.
- **`PendingMigrationsHealthCheck`** (`ready`): `GetPendingMigrationsAsync()`
  vacío. Evita servir tráfico con esquema desactualizado.
- **`OutboxHealthCheck`** (`ready`, solo si el outbox está activo): degraded si
  hay mensajes sin procesar más antiguos que `OutboxLagThreshold`; unhealthy si
  hay dead letters por encima de `DeadLetterThreshold`.
- **`TenantResolutionHealthCheck`** (`ready`, opcional): el almacén de tenants
  responde.

Cada check con su propio `CancellationToken` y timeout: un check colgado no debe
colgar el endpoint.

### 2.3 Formato de respuesta

`ResponseWriter` propio en JSON:

```json
{
  "status": "Degraded",
  "totalDurationMs": 42,
  "entries": {
    "database": { "status": "Healthy", "durationMs": 12 },
    "outbox":   { "status": "Degraded", "durationMs": 8, "description": "34 messages pending older than 00:05:00" }
  }
}
```

> **Seguridad**: `/health` detallado expone estructura interna (nombres de
> dependencias, estados, a veces mensajes de excepción). Por defecto:
> `/health/live` y `/health/ready` anónimos y **sin detalle** (solo el status);
> `/health` detallado requiere autorización (`HealthOptions.RequireAuthorization`,
> por defecto `true`) o queda restringido a red interna. Nunca incluir el mensaje
> de excepción en la respuesta pública.

### 2.4 Opciones

```csharp
public sealed class HealthOptions
{
    public bool RequireAuthorizationForDetails { get; set; } = true;
    public string? DetailsPolicy { get; set; }
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan DatabaseSlowThreshold { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan OutboxLagThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int DeadLetterThreshold { get; set; } = 10;
}
```

### 2.5 Startup y migraciones

Complemento: `IHostedService` de arranque que marca la app como "no lista" hasta
que las migraciones terminan (`StartupHealthCheck` con flag en memoria). Evita
la ventana en la que el proceso responde pero el esquema aún no está.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Infrastructure/Health/DatabaseHealthCheck.cs`
- `src/Raftel.Infrastructure/Health/PendingMigrationsHealthCheck.cs`
- `src/Raftel.Infrastructure/Health/OutboxHealthCheck.cs`
- `src/Raftel.Infrastructure/Health/StartupHealthCheck.cs`
- `src/Raftel.Infrastructure/Health/HealthOptions.cs`
- `src/Raftel.Api.Server/Health/HealthEndpointExtensions.cs`
- `src/Raftel.Api.Server/Health/HealthResponseWriter.cs`
- `tests/Raftel.Infrastructure.Tests/Health/**`
- `tests/Raftel.Api.FunctionalTests/HealthCheckTests.cs`

**Modificados**
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `demo/**/Program.cs`
- `docs/database-configuration.md`

## 4. Plan de implementación

1. **(test)** `DatabaseHealthCheck` devuelve `Healthy` con conexión válida y
   `Unhealthy` con cadena de conexión rota, sin lanzar.
2. Implementar `DatabaseHealthCheck` con timeout.
3. **(test)** `PendingMigrationsHealthCheck` ⇒ `Unhealthy` con migraciones pendientes.
4. Implementar.
5. **(test funcional)** `/health/live` responde 200 aun con la base caída;
   `/health/ready` responde 503.
6. Implementar los endpoints, tags y writer.
7. **(test funcional)** `/health` sin autenticar ⇒ 401 con la opción por defecto;
   la respuesta de `/health/ready` no incluye mensajes de excepción.
8. `OutboxHealthCheck` (si [07](07-outbox-domain-events.md) ya está) + tests.
9. `StartupHealthCheck` + test.
10. Documentar sondas de Kubernetes de ejemplo en `docs/`.

## 5. Tests

- **Unit**: cada check en healthy / degraded / unhealthy.
- **Integration (Testcontainers)**: parar el contenedor de base de datos y ver el
  cambio de estado.
- **Functional**: códigos de estado, autorización del endpoint detallado,
  ausencia de datos sensibles en la respuesta pública.

## 6. Criterios de aceptación

- [ ] `/health/live` no toca la base de datos.
- [ ] `/health/ready` devuelve 503 cuando la base no responde o hay migraciones pendientes.
- [ ] El endpoint detallado exige autorización por defecto.
- [ ] Ninguna respuesta pública incluye stack traces ni cadenas de conexión.
- [ ] Un check colgado no cuelga el endpoint (timeout).

## 7. Alternativas descartadas

- **`AspNetCore.HealthChecks.*` (AspNetCore.Diagnostics.HealthChecks)**: catálogo
  enorme y muy útil, pero es dependencia externa y el proyecto la evita
  deliberadamente. Los checks necesarios son cuatro y triviales.
- **Un único endpoint `/health`**: mezcla liveness y readiness; una base caída
  provocaría reinicios en bucle del pod.
