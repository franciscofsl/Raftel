# Backlog de evolución de Raftel

Ruta ordenada desde el estado actual (micro-framework funcional, ~5.650 líneas
en `src/` + `tools/`, 258 tests) hasta un framework completo listo para
producción.

Cada archivo de esta carpeta describe **una feature completa**: motivación,
diseño, superficie de API, archivos a tocar, plan de implementación por pasos,
tests y criterios de aceptación.

## Cómo usar este backlog

- Los números marcan el **orden recomendado de implementación**. Las features
  posteriores asumen que las anteriores están hechas.
- Cada archivo es autocontenido: se puede abrir una rama por archivo.
- Antes de tocar código, leer el `CLAUDE.md` más cercano a la carpeta afectada.
- TDD: test antes que implementación. Commits con Conventional Commits.
- Toda feature que rompa la API pública debe añadir entrada en
  `BREAKING_CHANGES.md`.

## Orden

### Fase 1 — Base de producción (bloqueante)

| # | Feature | Capas | Rompe API |
|---|---|---|---|
| [01](01-error-tipado-y-mapeo-http.md) | `Error` tipado y mapeo `Result` → status HTTP | Domain, Api.Server | Sí |
| [02](02-propagacion-cancellationtoken.md) | Propagación de `CancellationToken` en el pipeline | Application, Api.Server | Sí |
| [03](03-paginacion-y-ordenacion.md) | Paginación y ordenación (`PagedResult`, `PageRequest`) | Domain, Application, Infrastructure, Api | No |
| [04](04-logging-estructurado.md) | Logging estructurado y correlation ID | Application, Api.Server | No |
| [05](05-transacciones-explicitas.md) | Transacciones explícitas en `IUnitOfWork` | Application, Infrastructure | No |

### Fase 2 — Robustez

| # | Feature | Capas | Rompe API |
|---|---|---|---|
| [06](06-result-en-lugar-de-excepciones.md) | `Result` en lugar de excepciones en el pipeline | Application, Api.Server | Sí |
| [07](07-outbox-domain-events.md) | Outbox para eventos de dominio | Domain, Application, Infrastructure | No |
| [08](08-concurrencia-optimista.md) | Concurrencia optimista (`RowVersion`) | Domain, Infrastructure | No |
| [09](09-health-checks.md) | Health checks | Infrastructure, Api.Server | No |
| [10](10-seeding-y-bootstrap.md) | Seeding y bootstrap de datos iniciales | Application, Infrastructure | No |

### Fase 3 — Ergonomía y rendimiento

| # | Feature | Capas | Rompe API |
|---|---|---|---|
| [11](11-caching-de-queries.md) | Caching de queries | Application, Infrastructure | No |
| [12](12-specifications-en-repositorio.md) | Specifications en el repositorio e `IRepository` genérico | Domain, Infrastructure | Sí |
| [13](13-rate-limiting-cors-security-headers.md) | Rate limiting, CORS y cabeceras de seguridad | Api.Server | No |
| [14](14-nullable-enable.md) | `Nullable enable` en toda la solución | Todas | Sí |

### Fase 4 — Plataforma

| # | Feature | Capas | Rompe API |
|---|---|---|---|
| [15](15-observabilidad-opentelemetry.md) | Observabilidad con OpenTelemetry | Infrastructure, Api.Server | No |
| [16](16-background-jobs.md) | Background jobs y tareas recurrentes | Application, Infrastructure | No |
| [17](17-integration-events-message-bus.md) | Integration events y message bus | Application, Infrastructure | No |
| [18](18-raftel-testing.md) | Paquete `Raftel.Testing` | Nuevo proyecto | No |
| [19](19-templates-y-cli.md) | Templates `dotnet new` y CLI extendido | tools, templates | No |
| [20](20-localizacion-de-errores.md) | Localización de mensajes de error | Domain, Application, Api.Server | No |
| [21](21-flujos-de-autenticacion-adicionales.md) | Flujos de autenticación adicionales | Infrastructure, Api.Server | No |

## Estado

Ninguna feature implementada. Marcar el estado en la cabecera de cada archivo al
empezar (`Pendiente` → `En curso` → `Implementado`).
