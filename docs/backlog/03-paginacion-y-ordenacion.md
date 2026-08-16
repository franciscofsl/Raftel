# 03 — Paginación y ordenación

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Application`, `Raftel.Infrastructure`, `Raftel.Api.Server`, `Raftel.Api.Client`
> Rompe API pública: no (aditivo)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [02](02-propagacion-cancellationtoken.md)

## 1. Motivación

`IRepository.ListAllAsync` materializa la tabla entera:

```csharp
Task<List<TEntity>> ListAllAsync(Expression<Func<TEntity, bool>> filter = null, CancellationToken cancellationToken = default);
```

`GetAllTenantsQuery` devuelve todos los tenants. Con 100.000 filas, la API cae.
No existe `PagedResult`, `PageRequest` ni `Sort` en toda la solución.

Asimetría llamativa: `Raftel.Api.Client.QueryFilter` **sí** sabe construir query
strings desde un objeto (`FromObject`), pero no hay nada del lado servidor que
consuma `page`, `pageSize` ni `sort`. La mitad cliente del contrato existe sin
su contraparte.

## 2. Diseño

### 2.1 Tipos de dominio

En `Raftel.Domain.Abstractions` (sin dependencia de EF ni HTTP).

```csharp
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public int Page { get; }       // 1-based
    public int PageSize { get; }

    private PageRequest(int page, int pageSize) { Page = page; PageSize = pageSize; }

    public static Result<PageRequest> Create(int page = 1, int pageSize = DefaultPageSize)
    {
        if (page < 1)
            return Result.Failure<PageRequest>(Error.Validation("Page.Invalid", "Page must be greater than zero."));
        if (pageSize is < 1 or > MaxPageSize)
            return Result.Failure<PageRequest>(Error.Validation("PageSize.Invalid", $"Page size must be between 1 and {MaxPageSize}."));

        return Result.Success(new PageRequest(page, pageSize));
    }

    public int Skip => (Page - 1) * PageSize;
}
```

`MaxPageSize` es la defensa contra `?pageSize=999999`. Valor por defecto
sobrescribible vía opciones (§2.5).

```csharp
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public long TotalCount { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalCount) { ... }

    public static PagedResult<T> Empty(PageRequest request) => new([], request.Page, request.PageSize, 0);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
```

`Map` evita que cada handler repita la conversión entidad → response.

### 2.2 Ordenación

```csharp
public enum SortDirection { Ascending, Descending }

public sealed record SortRequest(string Field, SortDirection Direction = SortDirection.Ascending)
{
    // "name", "-createdAt", "name,-createdAt"
    public static Result<IReadOnlyList<SortRequest>> Parse(string? raw) { ... }
}
```

Sintaxis elegida: prefijo `-` para descendente (convención JSON:API). Compacta y
sin necesidad de un segundo parámetro `sortDirection`.

**Seguridad**: `SortRequest.Field` viene del cliente. Nunca construir la
expresión con string interpolation. Resolución por reflexión con **lista blanca**
por entidad:

```csharp
public sealed class SortMap<TEntity>
{
    private readonly Dictionary<string, Expression<Func<TEntity, object>>> _allowed;

    public SortMap<TEntity> Allow(string field, Expression<Func<TEntity, object>> selector);
    public Result<Expression<Func<TEntity, object>>> Resolve(string field);   // NotFound ⇒ Error.Validation
}
```

Un campo no declarado devuelve `Error.Validation("Sort.UnknownField", ...)`, no
una excepción ni un orden silenciosamente ignorado.

### 2.3 Repositorio

Método nuevo, `ListAllAsync` se conserva (útil para catálogos pequeños) pero se
marca `[Obsolete]` en su forma sin filtro cuando el conjunto puede crecer — o al
menos se documenta el riesgo.

```csharp
// Raftel.Domain/Abstractions/IRepository.cs
Task<PagedResult<TEntity>> ListPagedAsync(
    PageRequest page,
    Expression<Func<TEntity, bool>>? filter = null,
    IReadOnlyList<SortRequest>? sort = null,
    CancellationToken cancellationToken = default);
```

Implementación en `EfRepository`:

```csharp
public async Task<PagedResult<TEntity>> ListPagedAsync(
    PageRequest page,
    Expression<Func<TEntity, bool>>? filter = null,
    IReadOnlyList<SortRequest>? sort = null,
    CancellationToken cancellationToken = default)
{
    var query = dbContext.Set<TEntity>().WhereIf(filter is not null, filter);

    var totalCount = await query.LongCountAsync(cancellationToken);
    if (totalCount == 0) return PagedResult<TEntity>.Empty(page);

    query = ApplySort(query, sort);          // orden estable: siempre termina por Id

    var items = await query
        .Skip(page.Skip)
        .Take(page.PageSize)
        .ToListAsync(cancellationToken);

    return new PagedResult<TEntity>(items, page.Page, page.PageSize, totalCount);
}
```

**Orden estable obligatorio.** Sin `ORDER BY` determinista, `Skip/Take` puede
repetir u omitir filas entre páginas. `ApplySort` siempre añade
`ThenBy(e => e.Id)` como desempate final, incluso si el cliente no pidió orden.

**Dos consultas** (count + page) en vez de una con window function: portable
entre SqlServer y PostgreSQL sin SQL específico de proveedor. Se ejecutan dentro
del mismo `DbContext`, así que comparten filtros globales (soft delete, tenant).

### 2.4 Query base y endpoints

```csharp
// Raftel.Application/Queries/IPagedQuery.cs
public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    int? Page { get; }
    int? PageSize { get; }
    string? Sort { get; }
}
```

`QueryEndpointMapper.BuildRequestFromRouteAndQuery` ya resuelve parámetros del
constructor desde ruta y query string, así que `page`, `pageSize` y `sort` se
enlazan sin cambios si el record los declara. Lo que sí hay que añadir:

- `ApiParametersBuilder` debe documentar `page`/`pageSize`/`sort` en OpenAPI
  cuando el request implemente `IPagedQuery<>`.
- Cabeceras de respuesta opcionales `X-Total-Count` y `X-Total-Pages`, además
  del cuerpo.

### 2.5 Opciones

```csharp
public sealed class PaginationOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 200;
}
```

Registradas en `AddRaftelApplication` vía `IRaftelApplicationBuilder`.

### 2.6 Cliente

`Raftel.Api.Client` gana un helper simétrico:

```csharp
public static QueryFilter Paged(this QueryFilter filter, int page, int pageSize, string? sort = null);
```

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Abstractions/PageRequest.cs`
- `src/Raftel.Domain/Abstractions/PagedResult.cs`
- `src/Raftel.Domain/Abstractions/SortRequest.cs`
- `src/Raftel.Domain/Abstractions/SortDirection.cs`
- `src/Raftel.Domain/Specifications/SortMap.cs`
- `src/Raftel.Application/Queries/IPagedQuery.cs`
- `src/Raftel.Application/PaginationOptions.cs`
- `tests/Raftel.Domain.Tests/Abstractions/{PageRequestTests,PagedResultTests,SortRequestTests}.cs`
- `tests/Raftel.Infrastructure.Tests/Data/EfRepositoryPagingTests.cs`
- `tests/Raftel.Api.FunctionalTests/PaginationTests.cs`

**Modificados**
- `src/Raftel.Domain/Abstractions/IRepository.cs`
- `src/Raftel.Infrastructure/Data/EfRepository.cs`
- `src/Raftel.Api.Server/AutoEndpoints/ApiParametersBuilder.cs`
- `src/Raftel.Api.Server/AutoEndpoints/QueryEndpointMapper.cs`
- `src/Raftel.Application/Features/Tenants/GetAllTenants/*` — migrar a paginado
- `src/Raftel.Api.Client/QueryFilter.cs`
- `demo/**` — al menos una query paginada como ejemplo documentado

## 4. Plan de implementación

1. **(test)** `PageRequest.Create` rechaza página 0, negativa, tamaño 0 y tamaño
   > `MaxPageSize`, devolviendo `Error.Validation`.
2. Implementar `PageRequest`, `PagedResult`, `SortRequest`, `SortDirection`.
3. **(test)** `PagedResult.TotalPages` con resto (11 elementos, tamaño 5 ⇒ 3
   páginas), `HasNext`/`HasPrevious` en primera, intermedia y última página.
4. **(test)** `SortRequest.Parse` con `"name"`, `"-createdAt"`, `"a,-b"`, cadena
   vacía y campo desconocido.
5. Implementar `SortMap` con lista blanca.
6. **(test integración, Testcontainers)** `ListPagedAsync` devuelve la página
   correcta, `TotalCount` correcto, y **páginas disjuntas** (recorrer todas las
   páginas y comprobar que la unión no tiene duplicados) — cubre el orden estable.
7. Implementar `ListPagedAsync` en `EfRepository`.
8. **(test integración)** Los filtros globales de soft delete y tenant se aplican
   también al `LongCountAsync`.
9. Migrar `GetAllTenantsQuery` a `IPagedQuery<TenantResponse>`.
10. **(test funcional)** `GET /api/tenants?page=2&pageSize=5&sort=-name` devuelve
    la página correcta y `X-Total-Count`.
11. OpenAPI: parámetros documentados.
12. Helper en `Raftel.Api.Client` + tests.

## 5. Tests

- **Unit (Domain)**: validación, cálculo de páginas, `Map`, parseo de sort.
- **Unit**: `SortMap` rechaza campos no declarados (test de seguridad).
- **Integration**: paginación real contra SqlServer y PostgreSQL, páginas
  disjuntas, interacción con filtros globales.
- **Functional**: contrato HTTP, cabeceras, OpenAPI.

## 6. Criterios de aceptación

- [ ] Ninguna query del framework devuelve colecciones sin límite.
- [ ] `pageSize` por encima del máximo devuelve `400` con `Error.Validation`, no
      se recorta en silencio.
- [ ] Un campo de orden no permitido devuelve `400`, no se ignora.
- [ ] Recorrer todas las páginas devuelve exactamente el conjunto completo, sin
      duplicados ni huecos.
- [ ] OpenAPI documenta `page`, `pageSize` y `sort`.

## 7. Alternativas descartadas

- **Paginación por cursor (keyset)**: más eficiente en tablas grandes y sin el
  problema de deriva entre páginas, pero no permite salto directo a página N ni
  `TotalCount`, que es lo que espera una UI con paginador numérico. Candidata a
  feature futura **adicional**, no sustitutiva.
- **`IQueryable<T>` expuesto desde el repositorio**: filtra el detalle de EF
  Core hacia la capa de aplicación y rompe la regla de dependencias.
- **Sort por string interpolado en `OrderBy`**: superficie de inyección.
- **Una sola consulta con `COUNT(*) OVER()`**: ata el repositorio a SQL
  específico del proveedor.

## 8. Breaking changes

Ninguno. `ListAllAsync` se conserva. La migración de `GetAllTenantsQuery` sí
cambia el contrato de ese endpoint concreto: documentar en `BREAKING_CHANGES.md`.
