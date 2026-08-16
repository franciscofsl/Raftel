# 11 — Caching de queries

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (opt-in por query)
> Depende de: [02](02-propagacion-cancellationtoken.md), [04](04-logging-estructurado.md), [07](07-outbox-domain-events.md)

## 1. Motivación

Ninguna abstracción de caché en la solución. Toda query golpea la base de datos,
incluidas las que devuelven datos casi inmutables: catálogo de permisos, roles,
configuración del tenant, perfil del usuario.

El pipeline de middlewares es el sitio natural para resolverlo: `IQueryMiddleware<,>`
ya existe y se aplica solo a queries.

## 2. Diseño

### 2.1 Abstracción

```csharp
// Raftel.Application/Abstractions/Caching/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
```

Dos implementaciones:

- `MemoryCacheService` sobre `IMemoryCache` (por defecto, cero configuración).
- `DistributedCacheService` sobre `IDistributedCache` (Redis, SQL Server) para
  escenarios multi-instancia. Ambas viven en `Raftel.Infrastructure` y usan solo
  abstracciones de `Microsoft.Extensions.Caching.*`, sin atar a un proveedor.

### 2.2 Query cacheable

```csharp
// Raftel.Application/Queries/ICacheableQuery.cs
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration => null;          // null ⇒ usar el valor por defecto de opciones
    IReadOnlyList<string> Tags => [];      // para invalidación por grupo
}
```

Ejemplo:

```csharp
public sealed record GetUserProfileQuery(Guid UserId) : IQuery<GetUserProfileResponse>, ICacheableQuery
{
    public string CacheKey => $"user-profile:{UserId}";
    public IReadOnlyList<string> Tags => ["users", $"user:{UserId}"];
}
```

### 2.3 `CachingMiddleware`

```csharp
public sealed class CachingMiddleware<TQuery, TResponse>(ICacheService cache, ...)
    : IQueryMiddleware<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, RequestHandlerDelegate<Result<TResponse>> next, CancellationToken cancellationToken)
    {
        if (query is not ICacheableQuery cacheable)
        {
            return await next(cancellationToken);
        }

        var key = BuildKey(cacheable);
        var cached = await cache.GetAsync<TResponse>(key, cancellationToken);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var result = await next(cancellationToken);

        if (result.IsSuccess)                                  // nunca cachear fallos
        {
            await cache.SetAsync(key, result.Value, cacheable.Expiration ?? options.DefaultExpiration, cancellationToken);
        }

        return result;
    }
}
```

### 2.4 Aislamiento de la clave — punto crítico

La clave **debe** incorporar tenant y, cuando la respuesta depende de permisos,
el usuario. Sin esto, un tenant puede leer datos cacheados de otro. Es la
vulnerabilidad clásica del caching en aplicaciones multitenant.

```csharp
private string BuildKey(ICacheableQuery cacheable) =>
    $"{options.KeyPrefix}:t:{currentTenant.Id?.ToString() ?? "host"}:{cacheable.CacheKey}";
```

Para queries cuyo resultado varía según el usuario, marcador adicional:

```csharp
public interface IUserScopedCacheableQuery : ICacheableQuery;   // añade :u:{userId} a la clave
```

**Test de seguridad obligatorio**: misma query, dos tenants ⇒ dos entradas
distintas y ningún cruce de datos.

### 2.5 Invalidación

Fuente de verdad: los eventos de dominio. Con [07](07-outbox-domain-events.md)
en su sitio, un handler genérico invalida por tag:

```csharp
public sealed class CacheInvalidationHandler<TEvent>(ICacheService cache) : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent, IInvalidatesCache
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        Task.WhenAll(domainEvent.CacheTagsToInvalidate.Select(tag => cache.RemoveByTagAsync(tag, cancellationToken)));
}
```

`IMemoryCache` no soporta tags de forma nativa: `MemoryCacheService` mantiene un
índice `tag → HashSet<key>` con su propio lock. En `IDistributedCache` se usa un
set por tag (en Redis, un `SET` real).

> Advertencia a documentar: con `MemoryCacheService` y varias instancias, la
> invalidación es **local**. Multi-instancia exige caché distribuida o un canal
> de invalidación (pub/sub). No prometer coherencia que no se da.

### 2.6 Opciones

```csharp
public sealed class CacheOptions
{
    public bool Enabled { get; set; } = true;
    public string KeyPrefix { get; set; } = "raftel";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaxExpiration { get; set; } = TimeSpan.FromHours(1);
}
```

### 2.7 Qué no cachear

Documentar la lista explícitamente:

- Queries que devuelven datos que el usuario acaba de modificar (read-your-writes).
- Resultados fallidos.
- Cualquier cosa con datos de autenticación (tokens, hashes).
- Queries paginadas con orden dinámico y cardinalidad alta (la clave explota).

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Abstractions/Caching/{ICacheService,ICacheableQuery,IUserScopedCacheableQuery,IInvalidatesCache}.cs`
- `src/Raftel.Application/Middlewares/CachingMiddleware.cs`
- `src/Raftel.Application/CacheOptions.cs`
- `src/Raftel.Infrastructure/Caching/{MemoryCacheService,DistributedCacheService,TagIndex}.cs`
- `tests/Raftel.Application.UnitTests/Middlewares/CachingMiddlewareTests.cs`
- `tests/Raftel.Infrastructure.Tests/Caching/**`

**Modificados**
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `Directory.Packages.props` — `Microsoft.Extensions.Caching.Memory` / `.Abstractions`
- `src/Raftel.Application/Features/Users/GetUserProfile/*` — ejemplo cacheado
- `demo/**`

## 4. Plan de implementación

1. **(test)** `CachingMiddleware`: query no cacheable ⇒ pasa siempre al handler.
2. **(test)** Segundo hit con la misma clave ⇒ el handler **no** se invoca.
3. **(test)** `Result` fallido no se cachea.
4. Implementar `ICacheService` + `MemoryCacheService` + middleware.
5. **(test de seguridad)** Misma query en tenant A y tenant B ⇒ resultados
   independientes. Y con `IUserScopedCacheableQuery`, usuarios distintos ⇒
   entradas distintas.
6. Implementar el aislamiento de clave.
7. **(test)** `RemoveByTagAsync` invalida todas las claves del tag y ninguna otra.
8. Implementar el índice de tags (con test de concurrencia).
9. `DistributedCacheService` + tests de integración con Redis en Testcontainers.
10. Handler de invalidación por evento de dominio + test end-to-end
    (modificar usuario ⇒ el perfil cacheado se refresca).
11. Documentar limitaciones multi-instancia y la lista de "qué no cachear".

## 5. Tests

- **Unit**: hit/miss, no cachear fallos, expiración.
- **Seguridad**: aislamiento por tenant y por usuario (bloqueante).
- **Concurrencia**: el índice de tags bajo carga paralela.
- **Integration**: invalidación end-to-end vía evento de dominio.

## 6. Criterios de aceptación

- [ ] Ninguna entrada de caché se comparte entre tenants.
- [ ] Los fallos nunca se cachean.
- [ ] La invalidación por tag no borra claves ajenas.
- [ ] `Enabled = false` desactiva el middleware por completo.
- [ ] Documentada la limitación de invalidación local en memoria.

## 7. Alternativas descartadas

- **Caché de segundo nivel de EF Core**: transparente pero opaco para depurar, y
  ataría el framework a una librería de terceros.
- **Cachear por atributo `[Cacheable(60)]`**: más cómodo, pero la clave se
  volvería mágica y el aislamiento por tenant, implícito. La interfaz obliga a
  pensar la clave, que es exactamente donde están los fallos de seguridad.
- **Response caching HTTP**: distinto nivel; no invalidable desde el dominio y
  problemático con respuestas por usuario.
