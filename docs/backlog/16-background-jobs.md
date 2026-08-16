# 16 — Background jobs y tareas recurrentes

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo)
> Depende de: [05](05-transacciones-explicitas.md), [07](07-outbox-domain-events.md), [15](15-observabilidad-opentelemetry.md)

## 1. Motivación

No hay `IHostedService`, `BackgroundService` ni scheduler en la solución. Toda
tarea diferida es responsabilidad del consumidor, que acaba montando su propio
mecanismo.

Necesidades ya generadas por el propio backlog:

- Limpieza de mensajes procesados del outbox ([07](07-outbox-domain-events.md)).
- Purga de logs de auditoría antiguos (el modelo `AuditLog` crece sin límite y no
  hay nada que lo pode).
- Recalcular proyecciones, enviar resúmenes, expirar tokens.

## 2. Diseño

Alcance deliberadamente modesto: un scheduler **in-process** con persistencia.
No compite con Hangfire ni Quartz; cubre el 80% de casos sin dependencias.

### 2.1 Contrato

```csharp
// Raftel.Application/Abstractions/Jobs/IJob.cs
public interface IJob
{
    string Name { get; }
    Task<Result> ExecuteAsync(JobContext context, CancellationToken cancellationToken = default);
}

public sealed record JobContext(Guid ExecutionId, DateTimeOffset ScheduledFor, int AttemptNumber, TenantId? TenantId);
```

Registro declarativo:

```csharp
services.AddRaftelJobs(jobs =>
{
    jobs.AddRecurring<OutboxCleanupJob>("0 3 * * *");           // cron: cada día a las 3:00
    jobs.AddRecurring<AuditLogRetentionJob>(TimeSpan.FromHours(6));
    jobs.AddRecurringPerTenant<TenantUsageReportJob>("0 0 * * 1");
});
```

### 2.2 Expresiones cron

Parser propio, mínimo (5 campos estándar, sin segundos ni extensiones). Es el
componente con más riesgo de bugs sutiles: se cubre con una batería amplia de
tests de tabla (expresión + instante ⇒ siguiente ejecución esperada), incluyendo
cambios de horario de verano.

**Todo se calcula en UTC.** El horario local con DST provoca ejecuciones dobles
o saltadas dos veces al año. Si el consumidor necesita hora local, lo declara
explícitamente con `TimeZoneInfo` y se documenta el riesgo.

### 2.3 Coordinación entre instancias

Con N réplicas, un job recurrente debe ejecutarse **una vez**, no N. Bloqueo
distribuido sobre la base de datos:

```sql
-- tabla JobLocks(JobName PK, LockedUntil, Owner)
```

Adquisición con `UPDATE ... WHERE JobName = @name AND (LockedUntil IS NULL OR LockedUntil < @now)`
y comprobación de filas afectadas. Renovación periódica mientras el job corre
(lease), liberación al terminar. Si el proceso muere, el lease expira y otra
instancia lo recoge.

### 2.4 Historial y reintentos

Tabla `JobExecutions(Id, JobName, StartedOn, CompletedOn, Status, Error, AttemptNumber, TenantId)`.

- Fallo ⇒ reintento con backoff exponencial hasta `MaxAttempts`.
- Un job que no termina en `Timeout` se cancela vía `CancellationToken`.
- Solapamiento: por defecto **prohibido** (`AllowConcurrentExecution = false`).
  Si la ejecución anterior sigue viva, la nueva se salta y se registra.

### 2.5 Ejecución y scope

Cada ejecución abre su propio scope de DI (`IServiceScopeFactory`): `DbContext`
scoped, `ICurrentTenant` fijado, `ICorrelationContext` con un ID nuevo, y una
`Activity` raíz para las trazas ([15](15-observabilidad-opentelemetry.md)).

Los jobs pueden despachar comandos (`ICommandDispatcher`), con lo que heredan
validación, transacción y auditoría del pipeline. Es la vía recomendada:
**el job orquesta, el comando hace**.

### 2.6 Opciones

```csharp
public sealed class JobOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan ExecutionRetention { get; set; } = TimeSpan.FromDays(30);
}
```

`Enabled = false` permite desplegar réplicas de solo-API sin scheduler, patrón
habitual (API pods + worker pod).

### 2.7 Jobs incluidos

- `OutboxCleanupJob` — borra mensajes procesados fuera de retención.
- `AuditLogRetentionJob` — poda `AuditLog`/`EntityChange`/`PropertyChange`
  antiguos. **Importante**: hoy la auditoría no tiene política de retención y
  crece indefinidamente; borrar en lotes para no bloquear la tabla.
- `JobExecutionCleanupJob` — poda su propio historial.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Abstractions/Jobs/{IJob,IJobScheduler,JobContext}.cs`
- `src/Raftel.Application/JobOptions.cs`
- `src/Raftel.Infrastructure/Jobs/{JobScheduler,JobRunner,CronExpression,JobLockStore}.cs`
- `src/Raftel.Infrastructure/Jobs/{JobExecution,JobLock}.cs` + configuraciones EF
- `src/Raftel.Infrastructure/Jobs/Builtin/{OutboxCleanupJob,AuditLogRetentionJob,JobExecutionCleanupJob}.cs`
- `tests/Raftel.Infrastructure.Tests/Jobs/CronExpressionTests.cs`
- `tests/Raftel.Application.IntegrationTests/JobsTests.cs`

**Modificados**
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `src/Raftel.Infrastructure/Data/RaftelDbContext.cs`
- `demo/**/Program.cs`

## 4. Plan de implementación

1. **(test de tabla)** `CronExpression.GetNextOccurrence` para ~40 combinaciones,
   incluyendo `*/15`, rangos, listas, fin de mes y cambio de horario.
2. Implementar el parser cron.
3. **(test)** El scheduler dispara el job en el instante calculado
   (`TimeProvider` falso — ya está registrado en el contenedor,
   `services.AddSingleton(TimeProvider.System)`).
4. Implementar `JobScheduler` + `JobRunner` con scope propio.
5. **(test integración)** Dos instancias del scheduler ⇒ el job se ejecuta **una
   sola vez** (test del bloqueo).
6. Implementar `JobLockStore` con lease y renovación.
7. **(test)** Job que falla ⇒ reintento con backoff; agotados los intentos ⇒
   registrado como fallido.
8. **(test)** Job que excede el timeout ⇒ cancelado.
9. **(test)** Sin solapamiento cuando la ejecución previa sigue viva.
10. Jobs incluidos + tests (con especial cuidado en el borrado por lotes de auditoría).
11. Trazas y métricas por ejecución.

## 5. Tests

- **Unit**: cron (la mayor superficie de bugs), backoff, política de solapamiento.
- **Integration**: bloqueo con dos schedulers concurrentes, ejecución por tenant,
  retención de auditoría sin bloquear la tabla.
- **Integration**: `Enabled = false` no arranca ningún job.

## 6. Criterios de aceptación

- [ ] Un job recurrente se ejecuta una sola vez con varias instancias desplegadas.
- [ ] Un job que falla no tumba el host.
- [ ] Los cálculos de cron son en UTC y están cubiertos por tests de DST.
- [ ] La auditoría deja de crecer sin límite.
- [ ] Cada ejecución tiene su traza y su correlación.
- [ ] Los jobs pueden desactivarse por configuración.

## 7. Alternativas descartadas

- **Hangfire / Quartz.NET**: maduros y completos, pero son dependencias externas
  grandes (Hangfire trae su propio esquema y dashboard). Se documenta cómo
  integrarlos para quien necesite más de lo que ofrece esta feature.
- **`IHostedService` a pelo por job**: sin persistencia, sin coordinación entre
  instancias, sin historial. Es lo que se quiere evitar.
- **Cron del sistema / CronJob de Kubernetes invocando un endpoint**: mezcla
  responsabilidades de despliegue y aplicación, y expone endpoints que hay que
  proteger.
