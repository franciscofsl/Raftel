# Plan de implementación: Eventos de Dominio (Domain Events)

> Estado: **Propuesta / Plan de implementación**
> Rama: `claude/determined-einstein-0g6idd`
> Ámbito: `Raftel.Domain`, `Raftel.Application`, `Raftel.Infrastructure` (+ demo y tests)

## 1. Motivación

Hoy no existe ningún rastro de `IDomainEvent`, `INotification` ni mecanismo de
despacho de eventos de dominio en el repositorio, pese a que el proyecto cita
**DDD** como objetivo de aprendizaje. Esto limita escenarios habituales:

- **Integración eventual entre features/módulos** sin acoplarlos
  (ej.: _"cuando se crea un `Tenant`, dar de alta su configuración por defecto"_).
- **Proyecciones de lectura** y side-effects (envío de correos, notificaciones,
  outbox) desacoplados del handler que muta el agregado.
- Mantener los handlers de comando **finos** (Object Calisthenics: máx. ~2
  dependencias) delegando los efectos secundarios a handlers de evento.

Los eventos de dominio son el complemento natural del `AggregateRoot<TId>`
existente: el agregado **registra** eventos mientras aplica reglas de negocio, y
el framework los **despacha tras el commit**, reutilizando exactamente el mismo
patrón ya usado para el Unit of Work (`UnitOfWorkMiddleware` commitea solo si el
`Result` es exitoso).

## 2. Principios de diseño

1. **El dominio no conoce infraestructura.** `IDomainEvent` y el registro de
   eventos viven en `Raftel.Domain` sin dependencias nuevas.
2. **Reutilizar el patrón mediador existente.** Los handlers de evento se
   autodescubren por ensamblado igual que `ICommandHandler` / `Validator<T>`.
3. **Despacho tras persistir (post-commit).** Un evento solo se despacha si la
   transacción se confirma con éxito, coherente con `UnitOfWorkMiddleware`.
4. **Mínima fricción para el consumidor.** Levantar un evento = una llamada
   (`RaiseDomainEvent(...)`) dentro de un método de negocio del agregado.
5. **Sin librerías externas** (sin MediatR/INotification): implementación propia,
   en línea con la filosofía del micro-framework.

## 3. Arquitectura propuesta

Flujo extremo a extremo:

```
HTTP → AutoEndpoint → CommandHandler → Aggregate.RaiseDomainEvent(evt)
                                          │ (evento queda pendiente en el agregado)
                                          ▼
                              UnitOfWorkMiddleware.CommitAsync()   ← persiste cambios
                                          │
                                          ▼
                        DomainEventsDispatcher (post-commit)       ← recolecta y despacha
                                          │
                                          ▼
                        IDomainEventHandler<TEvent>.HandleAsync()  ← side-effects / integración
```

Decisión clave — **¿dónde se recolectan y despachan los eventos?**

| Opción | Dónde | Pros | Contras |
|---|---|---|---|
| **A. Interceptor EF Core** (recomendada) | `Raftel.Infrastructure`, en `SavedChanges` del `DbContext` | Acceso directo al `ChangeTracker`; recolecta eventos de **todos** los agregados realmente persistidos; despacho garantizado post-commit | El despacho ocurre dentro del ciclo de `SaveChanges` |
| B. Middleware en Application | `Raftel.Application`, tras `unitOfWork.CommitAsync()` | Simétrico textual a `UnitOfWorkMiddleware` | Application **no** tiene acceso al `ChangeTracker`; obligaría a exponer los agregados rastreados a través de `IUnitOfWork`, filtrando una preocupación de persistencia |

**Recomendación: Opción A** (interceptor), porque el `ChangeTracker` es la única
fuente fiable de "qué agregados se persistieron" y ya es el patrón usado por
`AuditPropertiesInterceptor` / `TenantInterceptor`. La abstracción `IDomainEventsDispatcher`
vive en `Application` (para que el dominio/aplicación razonen sobre ella) y su
disparo se ancla en el interceptor de Infrastructure.

> Nota: el despacho **in-process y síncrono** es el alcance de esta fase. Un
> patrón **Outbox** (persistir los eventos en la misma transacción para entrega
> fiable) se documenta como evolución futura en §9.

## 4. Cambios por capa

### 4.1. `Raftel.Domain`

**Nuevos archivos** (en `BaseTypes/` o nuevo `DomainEvents/`):

- `Abstractions/IDomainEvent.cs`
  ```csharp
  namespace Raftel.Domain.Abstractions;

  /// <summary>Marca un evento de dominio inmutable. Implementar como record.</summary>
  public interface IDomainEvent
  {
      DateTimeOffset OccurredOn { get; }
  }
  ```
  - Convención: los eventos son `sealed record` inmutables, nombrados en pasado
    (`TenantCreated`, `PirateCrownedKing`), viven junto a su agregado
    (`Features/<Feature>/Events/`).

**Modificar** `BaseTypes/AggregateRoot.cs` — añadir el registro de eventos
pendientes:

```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : TypedId<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    // ctores existentes intactos
}
```

- `RaiseDomainEvent` es `protected`: solo el propio agregado registra sus eventos
  desde sus métodos de negocio (respeta encapsulación / Object Calisthenics).
- `DomainEvents` y `ClearDomainEvents` son `public` para que el dispatcher de
  infraestructura los lea y vacíe tras despachar.

> Sin dependencias nuevas en Domain; `EF Core` sigue prohibido aquí.

### 4.2. `Raftel.Application`

**Nuevos archivos** (en `Abstractions/DomainEvents/`):

- `IDomainEventHandler.cs`
  ```csharp
  public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
  {
      Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
  }
  ```
- `IDomainEventsDispatcher.cs`
  ```csharp
  public interface IDomainEventsDispatcher
  {
      Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
  }
  ```
- `DomainEventsDispatcher.cs` — implementación que resuelve por reflexión el/los
  `IDomainEventHandler<TConcreto>` de cada evento desde el `IServiceProvider` y los
  invoca. Maneja el caso de **cero o múltiples** handlers por evento.

**Modificar** `DependencyInjection.cs` — autodescubrir handlers de evento por
ensamblado, igual que ya se hace con handlers de comando/query y validadores:

- Añadir `typeof(IDomainEventHandler<>)` al array `handlerTypes` de
  `RegisterHandlers`, de modo que cada `IDomainEventHandler<TEvent>` quede
  registrado automáticamente (`AddTransient`).
- Registrar `IDomainEventsDispatcher` → `DomainEventsDispatcher` como `Scoped`.

> Application sigue dependiendo solo de Domain; no aparecen tipos de EF Core.

### 4.3. `Raftel.Infrastructure`

**Nuevo** `Data/Interceptors/DomainEventsDispatchInterceptor.cs`:

- Hereda de `SaveChangesInterceptor`, sobrescribe `SavedChangesAsync`
  (post-commit) — patrón idéntico a `AuditPropertiesInterceptor`.
- Recorre `context.ChangeTracker.Entries<IAggregateWithEvents>()` (ver nota
  abajo), recolecta `DomainEvents`, llama a `ClearDomainEvents()` y delega en
  `IDomainEventsDispatcher.DispatchAsync(...)`.
- Se resuelve `IDomainEventsDispatcher` vía `IServiceProvider` (inyectado igual
  que `ICurrentUser` en `AuditPropertiesInterceptor`).

**Detalle técnico — filtrar agregados con eventos.** `AggregateRoot<TId>` es
genérico, por lo que `ChangeTracker.Entries<AggregateRoot<...>>()` no es directo.
Solución: introducir una interfaz marcador **no genérica** que exponga los eventos.

Opción recomendada: declarar en Domain una interfaz mínima
`IHasDomainEvents` y que `AggregateRoot<TId>` la implemente:

```csharp
// Raftel.Domain/Abstractions/IHasDomainEvents.cs
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

Así el interceptor filtra con `ChangeTracker.Entries<IHasDomainEvents>()` sin
reflexión sobre genéricos.

**Modificar** `DependencyInjection.cs` (`AddDataAccess`):

- Registrar `DomainEventsDispatchInterceptor` (`AddScoped`).
- Añadirlo a `.AddInterceptors(...)` junto a `AuditPropertiesInterceptor` y
  `TenantInterceptor`.

### 4.4. Orden de ejecución de interceptores

`SavingChanges`: Audit/Tenant ya operan aquí (mutan shadow properties antes de
persistir). El despacho de eventos debe ir en `SavedChanges`/`SavedChangesAsync`
(post-persistencia) para garantizar la semántica "solo si commitea". Documentar
explícitamente que `DomainEventsDispatchInterceptor` **no** modifica entidades.

## 5. Demo (`Raftel.Demo.*`) — ejemplo canónico

Para mantener el demo como showcase del framework:

- `Raftel.Demo.Domain/Pirates/Events/PirateCrownedKing.cs`:
  ```csharp
  public sealed record PirateCrownedKing(PirateId PirateId) : IDomainEvent
  {
      public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
  }
  ```
- Modificar `Pirate.FoundOnePiece()` para `RaiseDomainEvent(new PirateCrownedKing(Id))`.
- `Raftel.Demo.Application/Pirates/.../PirateCrownedKingHandler.cs` implementando
  `IDomainEventHandler<PirateCrownedKing>` (ej.: log / proyección de ejemplo).

## 6. Tests (TDD — test antes que implementación)

### Dominio (`Raftel.Domain.Tests`)
- `AggregateRoot` registra eventos al llamar un método de negocio.
- `DomainEvents` es de solo lectura; `ClearDomainEvents` los vacía.
- `RaiseDomainEvent` no es accesible desde fuera del agregado.

### Aplicación (`Raftel.Application.UnitTests`)
- `DomainEventsDispatcher` invoca el handler correcto por tipo de evento.
- Evento sin handler → no falla (no-op).
- Evento con múltiples handlers → se invocan todos.
- `DependencyInjectionTests`: los `IDomainEventHandler<>` se autodescubren.

### Infraestructura (`Raftel.Infrastructure.Tests`)
- Tras `CommitAsync`, los eventos de los agregados rastreados se despachan.
- Si la transacción no se confirma, no se despacha nada.
- Los eventos se vacían del agregado tras despacharse (no se reenvían en el
  siguiente `SaveChanges`).
- (Testcontainers/PostgreSql) test de integración end-to-end con el demo.

## 7. Pasos de implementación (orden sugerido, conventional commits)

1. `feat(domain): add IDomainEvent and domain event tracking to AggregateRoot`
   - `IDomainEvent`, `IHasDomainEvents`, cambios en `AggregateRoot`, tests de dominio.
2. `feat(application): add domain event handler abstraction and dispatcher`
   - `IDomainEventHandler<>`, `IDomainEventsDispatcher` + impl., autodescubrimiento
     en DI, tests de aplicación.
3. `feat(infrastructure): dispatch domain events after commit via interceptor`
   - `DomainEventsDispatchInterceptor`, registro en DI, tests de integración.
4. `feat(demo): showcase domain events with PirateCrownedKing`
   - evento + handler de ejemplo en el demo.
5. `docs: document domain events in CLAUDE.md hierarchy and docs/`
   - actualizar los `CLAUDE.md` de Domain/Application/Infrastructure y `docs/`.

## 8. Impacto y compatibilidad

- **Retrocompatible.** Los agregados existentes no cambian su comportamiento; la
  lista de eventos arranca vacía y solo se usa si un agregado llama a
  `RaiseDomainEvent`.
- **Sin nuevas dependencias de paquetes** (`Directory.Packages.props` sin cambios).
- **Regla de dependencias entre capas** intacta: Domain sigue sin EF Core; el
  despacho con `ChangeTracker` queda confinado a Infrastructure.
- Actualizar la documentación: `src/Raftel.Domain/CLAUDE.md` (patrón de eventos en
  agregados), `src/Raftel.Application/CLAUDE.md` (handlers de evento + dispatcher),
  `src/Raftel.Infrastructure/CLAUDE.md` (interceptor de despacho) y el `README.md`
  raíz (mención de Domain Events como capacidad del framework).

## 9. Evolución futura (fuera del alcance de esta fase)

- **Outbox pattern**: persistir los eventos en la misma transacción y despacharlos
  asíncronamente para entrega fiable e idempotente.
- **Eventos de integración**: mapear eventos de dominio a eventos de integración
  publicados a un bus (RabbitMQ/Kafka) para comunicación entre microservicios.
- **Despacho antes vs. después del commit configurable** (pre-commit para eventos
  que deban participar en la misma transacción).
- **Reglas de re-entrada**: handlers que levantan nuevos eventos / mutan agregados
  y disparan un nuevo `SaveChanges`.

## 10. Criterios de aceptación

- [ ] Un agregado puede registrar eventos desde un método de negocio sin exponer
      la colección a mutación externa.
- [ ] Los eventos se despachan **solo** cuando la transacción se confirma.
- [ ] Los handlers de evento se autodescubren por ensamblado (sin registro manual).
- [ ] El demo "Pirates" muestra un evento de dominio y su handler.
- [ ] Domain sigue sin dependencias a EF Core ni a otras capas.
- [ ] Cobertura de tests en las tres capas (dominio, aplicación, infraestructura).
- [ ] `dotnet build Raftel.sln` y `dotnet test` en verde.
