# 07 — Outbox para eventos de dominio

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo, opt-in)
> Depende de: [04](04-logging-estructurado.md), [05](05-transacciones-explicitas.md)

## 1. Motivación

El despacho actual está en `DomainEventsDispatchInterceptor`:

```csharp
public override async ValueTask<int> SavedChangesAsync(...)
{
    await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
    return await base.SavedChangesAsync(eventData, result, cancellationToken);
}
```

Es decir: **in-process, síncrono, después del `SaveChanges`**. Riesgos:

1. **Evento perdido.** Si el handler lanza, el evento desaparece. No hay
   reintento ni registro de lo ocurrido. Si además el proceso muere entre el
   commit y el despacho, igual.
2. **Excepción del handler contamina el comando.** La excepción sube por
   `SaveChangesAsync` y hace fallar un comando que **ya persistió**. El cliente
   recibe 500 sobre una operación que sí ocurrió.
3. **Latencia acoplada.** Un handler que manda un email añade su latencia a la
   respuesta HTTP.
4. **Sin trazabilidad.** No se puede responder a "¿se procesó el evento X?".

El propio `docs/domain-events-plan.md` menciona outbox como evolución natural.

## 2. Diseño

Patrón **Transactional Outbox**: el evento se persiste en la misma transacción
que el cambio de estado; un procesador lo despacha después con reintentos.

```
Comando ─┬─ muta agregado
         └─ eventos → tabla OutboxMessages   } una sola transacción
                              │
                              ▼
                    OutboxProcessor (BackgroundService)
                              │
                              ▼
                    IDomainEventHandler<T>   (con reintentos)
```

### 2.1 Modelo

```csharp
// Raftel.Domain/Outbox/OutboxMessage.cs
public sealed class OutboxMessage : Entity<OutboxMessageId>
{
    public string Type { get; private set; }          // AssemblyQualifiedName acotado
    public string Content { get; private set; }       // JSON del evento
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public int AttemptCount { get; private set; }
    public string? Error { get; private set; }
    public Guid? TenantId { get; private set; }       // el handler debe correr en el tenant correcto
    public string? CorrelationId { get; private set; }

    public void MarkProcessed(DateTimeOffset now);
    public void MarkFailed(DateTimeOffset now, string error);
}
```

Guardar `TenantId` y `CorrelationId` es esencial: el procesador corre fuera de la
petición HTTP, sin `HttpContext`, así que no hay tenant ni correlación ambiental.

### 2.2 Escritura: cambiar el interceptor

`DomainEventsDispatchInterceptor` pasa a `OutboxWriteInterceptor` y actúa en
**`SavingChanges`** (antes del commit), no en `SavedChanges`:

```csharp
public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken)
{
    var context = eventData.Context;
    var messages = context.ChangeTracker
        .Entries<IHasDomainEvents>()
        .SelectMany(entry => Drain(entry.Entity))
        .Select(ToOutboxMessage)
        .ToList();

    context.Set<OutboxMessage>().AddRange(messages);   // misma transacción
    return base.SavingChangesAsync(eventData, result, cancellationToken);
}
```

Detalle: añadir entidades durante `SavingChanges` es válido en EF Core; los
`OutboxMessage` entran en el mismo `SaveChanges`.

### 2.3 Serialización

`System.Text.Json` con `JsonSerializerOptions` propias del outbox. El `Type` se
guarda como `"Namespace.EventName, Assembly"` (sin versión ni token) para
sobrevivir a actualizaciones de versión del ensamblado.

Resolución de tipo con **lista blanca**: solo tipos que implementen `IDomainEvent`
en los ensamblados registrados. Deserializar un tipo arbitrario desde la base es
un vector de ejecución si alguien puede escribir en la tabla.

### 2.4 Procesador

```csharp
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
```

Bucle:

1. Tomar hasta `BatchSize` mensajes con `ProcessedOn IS NULL` y
   `AttemptCount < MaxAttempts`, ordenados por `OccurredOn`.
2. Bloqueo para evitar doble procesado con varias instancias:
   `UPDATE ... SET LockedUntil = ... OUTPUT INSERTED.*` (SqlServer) /
   `SELECT ... FOR UPDATE SKIP LOCKED` (PostgreSQL). Abstraído tras
   `IOutboxStore` con implementación por proveedor.
3. Por cada mensaje: crear scope de DI, fijar el tenant (`ICurrentTenant`) y la
   correlación, deserializar, resolver `IDomainEventHandler<T>`, ejecutar.
4. Éxito ⇒ `MarkProcessed`. Fallo ⇒ `MarkFailed`, incrementar `AttemptCount`,
   backoff exponencial (`Delay = Base * 2^Attempt`, con tope).
5. Agotados los intentos ⇒ el mensaje queda como dead letter (consultable).

**Idempotencia**: la entrega es *at-least-once*. Documentar que los handlers
deben ser idempotentes. Ofrecer `IIdempotencyStore` opcional que registre
`(MessageId, HandlerType)` procesados.

### 2.5 Opciones y modo de compatibilidad

```csharp
public sealed class OutboxOptions
{
    public bool Enabled { get; set; } = false;                       // opt-in
    public int BatchSize { get; set; } = 50;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxAttempts { get; set; } = 5;
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);  // limpieza de procesados
}
```

`Enabled = false` mantiene el comportamiento in-process actual. Migración sin
sorpresas para quien ya use eventos de dominio.

### 2.6 Limpieza

Job de retención que borra `ProcessedOn < now - RetentionPeriod`. Sin él la tabla
crece sin límite. Se apoya en [16](16-background-jobs.md) si ya existe; si no,
un `BackgroundService` propio.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Outbox/OutboxMessage.cs`
- `src/Raftel.Domain/Outbox/ValueObjects/OutboxMessageId.cs`
- `src/Raftel.Application/Abstractions/Outbox/{IOutboxStore,IOutboxSerializer,IIdempotencyStore}.cs`
- `src/Raftel.Application/OutboxOptions.cs`
- `src/Raftel.Infrastructure/Outbox/{OutboxProcessor,OutboxStore,OutboxSerializer,OutboxCleanupService}.cs`
- `src/Raftel.Infrastructure/Data/Interceptors/OutboxWriteInterceptor.cs`
- `src/Raftel.Infrastructure/Data/Configuration/Outbox/OutboxMessageConfiguration.cs`
- `tests/Raftel.Infrastructure.Tests/Outbox/**`
- `tests/Raftel.Application.IntegrationTests/OutboxTests.cs`

**Modificados**
- `src/Raftel.Infrastructure/Data/RaftelDbContext.cs` — `DbSet<OutboxMessage>`
- `src/Raftel.Infrastructure/Data/Interceptors/DomainEventsDispatchInterceptor.cs` — condicionar a `!Outbox.Enabled`
- `src/Raftel.Infrastructure/DependencyInjection.cs`

## 4. Plan de implementación

1. **(test)** `OutboxMessage` transiciona correctamente (procesado, fallido,
   contador de intentos).
2. Modelo + configuración EF + índice
   `(ProcessedOn, OccurredOn) WHERE ProcessedOn IS NULL` (filtrado).
3. **(test)** Serializador: round-trip de un evento; tipo desconocido ⇒ error
   controlado, no excepción de deserialización.
4. Implementar serializador con lista blanca.
5. **(test integración)** Comando con evento y `Outbox.Enabled = true` ⇒ fila en
   `OutboxMessages` dentro de la misma transacción; rollback ⇒ **sin** fila.
6. Implementar `OutboxWriteInterceptor`.
7. **(test integración)** Procesador despacha, marca procesado y no repite.
8. **(test integración)** Handler que lanza ⇒ `AttemptCount` sube, `Error`
   registrado, reintento tras el backoff, dead letter al agotar intentos.
9. Implementar `OutboxProcessor` + `IOutboxStore` por proveedor.
10. **(test integración)** Tenant y correlación restaurados en el handler.
11. Servicio de limpieza + test.
12. Documentar garantías (at-least-once, idempotencia requerida).

## 5. Tests

- **Unit**: modelo, serializador, cálculo de backoff.
- **Integration (Testcontainers)**: atomicidad, reintentos, dead letter,
  restauración de tenant, bloqueo con dos procesadores concurrentes.
- **Integration**: con `Enabled = false`, comportamiento idéntico al actual
  (test de no regresión).

## 6. Criterios de aceptación

- [ ] Un evento nunca se pierde si la transacción commitea.
- [ ] Un handler que falla no hace fallar el comando.
- [ ] Los eventos se despachan en el tenant correcto.
- [ ] Los mensajes agotados quedan consultables como dead letter.
- [ ] Dos instancias del procesador no despachan el mismo mensaje.
- [ ] `Enabled = false` reproduce el comportamiento actual.

## 7. Alternativas descartadas

- **Change Data Capture / Debezium**: potente pero mete infraestructura externa,
  contra la filosofía "sin dependencias externas".
- **Cola en memoria (`Channel<T>`)**: pierde mensajes al reiniciar el proceso.
  Resuelve la latencia, no la durabilidad.
- **Despachar en `SavedChanges` dentro de la transacción de [05](05-transacciones-explicitas.md)**:
  mejora la consistencia de escrituras en base pero no protege de side-effects
  externos ni da reintentos.
- **Publicar directo a un broker desde el interceptor**: escritura dual sin
  atomicidad, el problema clásico que el outbox existe para resolver.
