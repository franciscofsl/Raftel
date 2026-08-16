# 02 — Propagación de `CancellationToken` en el pipeline

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Api.Server`, `Raftel.Infrastructure`
> Rompe API pública: **sí** (firma de `IGlobalMiddleware` y `RequestHandlerDelegate`)
> Depende de: nada (independiente de 01, pero se recomienda hacerla después)

## 1. Motivación

El token existe en los extremos pero **se pierde por el camino**:

```csharp
// IRequestHandler SÍ lo acepta
Task<TResponse> HandleAsync(TRequest request, CancellationToken token = default);

// ICommandDispatcher SÍ lo acepta... y lo descarta
public Task<Result> DispatchAsync<TCommand>(TCommand command, CancellationToken token = default)
    => dispatcher.DispatchAsync<TCommand, Result>(command);   // ← token perdido

// IRequestDispatcher NO lo tiene
Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request);

// RequestHandlerDelegate NO lo tiene
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

// IGlobalMiddleware NO lo tiene
Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next);
```

Resultado: **ninguna operación es cancelable**. Si el cliente aborta la petición
HTTP, la query sigue golpeando la base de datos hasta terminar. En endpoints
lentos o bajo carga esto es consumo puro de conexiones.

`IQueryDispatcher.DispatchAsync` ni siquiera declara el parámetro.

## 2. Diseño

Un único `CancellationToken` fluye endpoint → dispatcher → middlewares →
handler → repositorio → EF Core.

### 2.1 Firmas nuevas

```csharp
// Raftel.Application/Abstractions/RequestHandlerDelegate.cs
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);

// Raftel.Application/Abstractions/IRequestDispatcher.cs
Task<TResponse> DispatchAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    where TRequest : IRequest<TResponse>;

// Raftel.Application/Middlewares/IGlobalMiddleware.cs
Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);

// Raftel.Application/Queries/IQueryDispatcher.cs
Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
    where TQuery : IQuery<TResult>;
```

`ICommandMiddleware<T>`, `ICommandMiddleware<T,R>` e `IQueryMiddleware<T,R>`
heredan de `IGlobalMiddleware`, así que el cambio se propaga solo.

### 2.2 Composición del pipeline

`RequestDispatcher.DispatchAsync` reconstruye la cadena pasando el token:

```csharp
var handlerDelegate = new RequestHandlerDelegate<TResponse>(
    token => handler.HandleAsync(request, token));

var pipeline = allMiddlewares
    .Reverse<IGlobalMiddleware<TRequest, TResponse>>()
    .Aggregate(handlerDelegate,
        (next, middleware) => token => middleware.HandleAsync(request, next, token));

return await pipeline(cancellationToken);
```

> Cuidado con la captura de variables en el `Aggregate`: `next` debe capturarse
> por valor en cada iteración (lo hace el lambda del `Aggregate`, pero conviene
> un test que registre 3 middlewares y verifique el orden de ejecución).

### 2.3 Origen del token en los endpoints

Minimal API inyecta `CancellationToken` como parámetro del handler y lo enlaza a
`HttpContext.RequestAborted`. Los mappers pasan a:

```csharp
async Task<IResult> Handler(HttpContext context, ICommandDispatcher dispatcher, CancellationToken cancellationToken)
{
    // ...
    var result = await dispatcher.DispatchAsync(parsedCommand, cancellationToken);
    // ...
}
```

### 2.4 Repositorios y handlers

`IRepository` ya acepta token en todos sus métodos. Lo que falta es que los
handlers lo **pasen**: hoy la mayoría llama `GetByIdAsync(id)` sin token.
Barrido en `src/Raftel.Application/Features/**` y `demo/Raftel.Demo.Application/**`.

### 2.5 Cancelación en el commit

Regla: **`UnitOfWorkMiddleware` NO propaga el token al commit.** Cancelar a
mitad de un `SaveChanges` deja la transacción en estado indeterminado y aborta
el despacho de eventos de dominio. El commit se ejecuta con
`CancellationToken.None` y se documenta en el XML doc del middleware.

## 3. Archivos a tocar

**Modificados**
- `src/Raftel.Application/Abstractions/RequestHandlerDelegate.cs`
- `src/Raftel.Application/Abstractions/IRequestDispatcher.cs`
- `src/Raftel.Application/Abstractions/RequestDispatcher.cs`
- `src/Raftel.Application/Commands/ICommandDispatcher.cs` + `CommandDispatcher.cs`
- `src/Raftel.Application/Queries/IQueryDispatcher.cs` + `QueryDispatcher.cs`
- `src/Raftel.Application/Middlewares/IGlobalMiddleware.cs`
- `src/Raftel.Application/Middlewares/ICommandMiddleware.cs`, `IQueryMiddleware.cs`
- `src/Raftel.Application/Middlewares/{Validation,UnitOfWork,PermissionAuthorization,AuditLog}Middleware.cs`
- `src/Raftel.Application/Features/**` — todos los handlers
- `src/Raftel.Api.Server/AutoEndpoints/{Command,Query}EndpointMapper.cs`
- `src/Raftel.Application/Abstractions/DomainEvents/IDomainEventsDispatcher.cs` (ya lo tiene, verificar propagación)
- `demo/**` y `tests/**` afectados
- `BREAKING_CHANGES.md`

## 4. Plan de implementación

1. **(test)** `RequestDispatcherTests`: con un token ya cancelado, el handler
   recibe `token.IsCancellationRequested == true`.
2. **(test)** Orden de middlewares preservado tras el cambio de firma (3
   middlewares que escriben en una lista compartida).
3. Cambiar `RequestHandlerDelegate` e `IGlobalMiddleware`. La solución deja de
   compilar: es la guía de trabajo.
4. Actualizar los 4 middlewares del framework.
5. Actualizar los 3 dispatchers.
6. Actualizar mappers de endpoint (añadir `CancellationToken` al `Handler`).
7. Barrido de handlers: pasar el token a cada llamada de repositorio.
8. Actualizar demo y tests.
9. **(test funcional)** Petición abortada por el cliente ⇒ la operación no
   completa (verificable con un handler de prueba que espere sobre el token).

## 5. Tests

- **Unit**: propagación a través de una cadena de 3 middlewares.
- **Unit**: `UnitOfWorkMiddleware` commitea aun con token cancelado tras un
  `Result` exitoso (decisión §2.5).
- **Integration**: cancelar el token durante una query larga lanza
  `OperationCanceledException` desde EF Core.
- **Architecture test** (nuevo, útil como red permanente): todo método público
  `*Async` en `Raftel.Application` y `Raftel.Domain` declara un parámetro
  `CancellationToken`.

## 6. Criterios de aceptación

- [ ] Ninguna llamada a repositorio en `src/` ni `demo/` omite el token.
- [ ] `IQueryDispatcher.DispatchAsync` acepta token.
- [ ] Middlewares personalizados de terceros compilan tras ajustar la firma (documentado).
- [ ] Test de arquitectura que impide regresiones.
- [ ] `dotnet test` en verde.

## 7. Alternativas descartadas

- **`CancellationToken` dentro del objeto request**: contamina el contrato del
  comando/query, que es un DTO serializable desde JSON.
- **`AsyncLocal<CancellationToken>` ambiental**: implícito, difícil de testear,
  problemático con el pooling de hilos.
- **Sobrecargas nuevas conservando las viejas**: duplica todo el pipeline y deja
  el camino sin token vivo indefinidamente.

## 8. Breaking changes

- `IGlobalMiddleware.HandleAsync` gana un tercer parámetro.
- `RequestHandlerDelegate<T>` pasa a aceptar `CancellationToken`.
- Cualquier middleware definido fuera del framework debe actualizar su firma.
