# 17 — Integration events y message bus

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo, opt-in)
> Depende de: [07](07-outbox-domain-events.md), [15](15-observabilidad-opentelemetry.md), [16](16-background-jobs.md)

## 1. Motivación

Los eventos de dominio actuales son **in-process**: `DomainEventsDispatcher`
resuelve `IDomainEventHandler<T>` del contenedor de DI y lo invoca. No hay forma
de que otro servicio se entere de nada.

Distinción que el framework aún no hace:

| | Domain event | Integration event |
|---|---|---|
| Alcance | Dentro del mismo proceso y transacción | Entre servicios |
| Contrato | Interno, puede cambiar libremente | **Público y versionado** |
| Contenido | Puede llevar el agregado | Solo datos primitivos serializables |
| Entrega | Inmediata | Asíncrona, at-least-once |

Publicar el domain event tal cual al exterior es el error habitual: acopla el
modelo interno al contrato público y cualquier refactor rompe consumidores.

## 2. Diseño

### 2.1 Contratos

```csharp
// Raftel.Application/Abstractions/Messaging/IIntegrationEvent.cs
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }     // nombre estable y versionado: "raftel.users.created.v1"
}
```

El nombre lleva versión explícita desde el día uno. Añadir un campo ⇒ misma
versión; quitar o cambiar el significado de uno ⇒ `.v2`, publicando ambas
durante la transición.

```csharp
public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task<Result> HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

### 2.2 Traducción domain → integration

Explícita, nunca automática:

```csharp
public interface IIntegrationEventFactory<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    IIntegrationEvent Create(TDomainEvent domainEvent);
}
```

Así el autor decide qué se publica. Un `UserCreatedDomainEvent` con el agregado
completo se traduce a `UserCreatedIntegrationEvent(UserId, Email, TenantId)`.

### 2.3 Publicación vía outbox

Se **reutiliza** el outbox de [07](07-outbox-domain-events.md). No se construye
un segundo mecanismo: `OutboxMessage` gana una columna `Destination`
(`InProcess` | `Broker`), y el procesador enruta según el valor.

Garantía: el integration event se persiste en la misma transacción que el cambio
de estado, y se publica al broker con reintentos. Nunca escritura dual.

### 2.4 Transporte

```csharp
public interface IMessageTransport
{
    Task SendAsync(TransportMessage message, CancellationToken cancellationToken = default);
    Task StartConsumingAsync(Func<TransportMessage, CancellationToken, Task<Result>> handler, CancellationToken cancellationToken = default);
}
```

En el core: **solo la abstracción** y una implementación `InMemoryTransport`
(útil para tests y monolitos modulares). Los transportes reales van en paquetes
separados para no arrastrar dependencias:

- `Raftel.Messaging.RabbitMQ`
- `Raftel.Messaging.AzureServiceBus`

Cada paquete es opcional, con su propio ciclo de versiones.

### 2.5 Consumo

Servicio de fondo que consume del transporte, deserializa y despacha al
`IIntegrationEventHandler<T>` correspondiente.

Puntos obligatorios:

- **Idempotencia**: entrega at-least-once ⇒ tabla `ProcessedMessages(EventId, HandlerType)`
  con clave única. Un evento ya procesado se descarta.
- **Dead letter**: tras `MaxAttempts`, el mensaje va a una cola de descarte, no
  se pierde ni bloquea la cola.
- **Ordenación**: **no se garantiza**. Documentarlo explícitamente; los handlers
  deben tolerar desorden (por ejemplo, ignorar un evento más antiguo que el
  estado actual).
- **Envenenamiento**: un mensaje que siempre falla no debe bloquear el consumo
  del resto.

### 2.6 Seguridad

- El `EventType` recibido resuelve el tipo CLR contra una **lista blanca** de
  eventos registrados. Nunca `Type.GetType` sobre una cadena del broker.
- Límite de tamaño de mensaje; por encima, dead letter directo.
- Los mensajes entrantes son **datos no confiables**: se validan como cualquier
  entrada externa antes de tocar el dominio.
- Tenant: el evento lleva `TenantId` y el consumidor fija `ICurrentTenant` antes
  de despachar. Un evento sin tenant en un sistema multitenant se rechaza.

### 2.7 Trazas

`traceparent` viaja en las cabeceras del `TransportMessage`. El consumidor abre
la actividad con el contexto remoto como padre, de modo que la traza cruza el
límite del servicio ([15](15-observabilidad-opentelemetry.md)).

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Abstractions/Messaging/{IIntegrationEvent,IEventPublisher,IIntegrationEventHandler,IIntegrationEventFactory,IMessageTransport,TransportMessage}.cs`
- `src/Raftel.Application/MessagingOptions.cs`
- `src/Raftel.Infrastructure/Messaging/{EventPublisher,InMemoryTransport,IntegrationEventConsumer,ProcessedMessageStore,EventTypeRegistry}.cs`
- `tests/Raftel.Application.IntegrationTests/IntegrationEventsTests.cs`
- (paquetes separados) `src/Raftel.Messaging.RabbitMQ/**`

**Modificados**
- `src/Raftel.Domain/Outbox/OutboxMessage.cs` — columna `Destination`
- `src/Raftel.Infrastructure/Outbox/OutboxProcessor.cs` — enrutado
- `src/Raftel.Infrastructure/DependencyInjection.cs`

## 4. Plan de implementación

1. Definir contratos y `EventTypeRegistry` con lista blanca.
2. **(test)** Un `EventType` desconocido se rechaza sin intentar resolver el tipo.
3. Extender `OutboxMessage` con `Destination` y enrutar en el procesador.
4. **(test integración)** Un comando que levanta un domain event con factory
   asociada produce una fila de outbox con destino `Broker`.
5. Implementar `IEventPublisher` sobre outbox.
6. `InMemoryTransport` + **(test)** publicar y consumir extremo a extremo.
7. **(test)** Entrega duplicada del mismo `EventId` ⇒ el handler se ejecuta una
   sola vez.
8. Implementar `ProcessedMessageStore` con clave única.
9. **(test)** Mensaje envenenado va a dead letter sin bloquear la cola.
10. **(test)** Tenant restaurado en el consumidor; evento sin tenant rechazado.
11. Propagación de `traceparent` + test de traza distribuida.
12. Paquete de RabbitMQ con tests de integración (Testcontainers) — fuera del
    core, PR aparte.

## 5. Tests

- **Unit**: registro de tipos, serialización, idempotencia.
- **Integration**: publicación transaccional, consumo, duplicados, dead letter,
  aislamiento por tenant.
- **Integration (Testcontainers RabbitMQ)**: solo en el paquete del transporte.

## 6. Criterios de aceptación

- [ ] El core no depende de ningún broker.
- [ ] Un integration event se publica solo si la transacción commitea.
- [ ] Un evento entregado dos veces se procesa una sola vez.
- [ ] Los tipos se resuelven contra lista blanca.
- [ ] Los eventos se procesan en el tenant correcto.
- [ ] La traza cruza el límite del servicio.
- [ ] Documentado que no hay garantía de orden.

## 7. Alternativas descartadas

- **MassTransit / NServiceBus**: resuelven todo esto y mucho más, pero son
  dependencias pesadas con su propio modelo mental. Se documenta cómo usarlos en
  lugar de esta feature para quien los prefiera.
- **Publicar los domain events directamente al broker**: acopla el contrato
  público al modelo interno; cualquier refactor del agregado rompe consumidores.
- **Mecanismo de publicación separado del outbox**: duplicaría la lógica de
  reintentos y perdería la atomicidad.
