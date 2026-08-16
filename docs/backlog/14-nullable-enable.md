# 14 — `Nullable enable` en toda la solución

> Estado: **Pendiente**
> Ámbito: todos los proyectos
> Rompe API pública: **sí** (anotaciones de nulabilidad)
> Depende de: [01](01-error-tipado-y-mapeo-http.md) … [13](13-rate-limiting-cors-security-headers.md) — hacerlo al final de una fase, no a mitad

## 1. Motivación

**Los diez `csproj` de `src/`, `tools/` y `demo/` llevan `<Nullable>disable</Nullable>`.**
Un framework .NET 10 renunciando por completo al análisis de nulabilidad.

Efectos visibles hoy en el propio código:

```csharp
// IRepository — el ? no significa nada con nullable disabled
Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

// EfRepository
Task<List<TEntity>> ListAllAsync(Expression<Func<TEntity, bool>> filter = null, ...)
//                                                                        ^^^^^ null a un tipo no anotado

// Result<TValue>
[NotNull] public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException(...);
//        ^^^^^^^^ atributo de un análisis que está apagado
```

Es decir: el código **ya está escrito** con la intención de nulabilidad
(`?`, `!`, `[NotNull]`) pero el compilador no comprueba nada. Lo peor de ambos
mundos — ruido sintáctico sin garantía.

Para el consumidor del framework el impacto es directo: al referenciar
`Raftel.Domain` desde un proyecto con `nullable enable`, todos los tipos del
framework aparecen como "oblivious" y no aportan ninguna información al análisis.

## 2. Diseño

Migración **por proyecto, en orden de dependencia**, no de golpe. Un PR por
proyecto.

### 2.1 Orden

1. `Raftel.Shared` (sin dependencias, ~4 archivos)
2. `Raftel.Domain`
3. `Raftel.Application`
4. `Raftel.Api.Client`
5. `Raftel.Infrastructure`
6. `Raftel.Api.Server`
7. `Raftel.Cli`
8. `demo/**`
9. `tests/**` (opcional, menor prioridad)

### 2.2 Estrategia por proyecto

Paso intermedio para no ahogarse en avisos:

```xml
<Nullable>enable</Nullable>
<WarningsNotAsErrors>CS8600;CS8602;CS8603;CS8604;CS8618;CS8625</WarningsNotAsErrors>
```

Se activa, se cuentan los avisos, se corrigen por categoría, y al terminar el
proyecto se quita `WarningsNotAsErrors` y se añade:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Así el proyecto ya migrado no puede regresar.

### 2.3 Decisiones de anotación

Puntos donde hay que decidir el contrato, no solo callar avisos:

| Elemento | Decisión |
|---|---|
| `IRepository.GetByIdAsync` | `Task<TEntity?>` — ausencia esperada, ya está bien |
| `ListAllAsync(filter = null)` | `Expression<Func<TEntity,bool>>? filter = null` |
| `Entity.Id` | `TId` no nulo; el constructor sin parámetros (para EF) usa `= null!` con comentario |
| `Result<T>.Value` | Mantener `[NotNull]` + `!`; el contrato es "no nulo si `IsSuccess`" |
| `Error.Message`, `Error.Code` | No nulos |
| `ICurrentUser.Id` | `Guid?` — puede no haber usuario |
| `ICurrentTenant.Id` | `TenantId?` — modo host sin tenant |
| Propiedades de entidades EF | `= null!` en navegaciones requeridas, `?` en opcionales |

### 2.4 Anti-patrón a evitar

Prohibido "migrar" a base de `!` o `#nullable disable` local. Cada `!` debe
tener justificación: constructor de EF, o invariante garantizada por el flujo. Se
revisa en el PR.

### 2.5 Constructores para EF Core

`Entity` y `AggregateRoot` tienen constructores sin parámetros vacíos para el
materializador de EF. Con nullable activo generan CS8618. Patrón:

```csharp
#pragma warning disable CS8618 // EF Core materialization constructor
protected Entity() { }
#pragma warning restore CS8618
```

Preferible al `= null!` disperso por cada propiedad.

## 3. Archivos a tocar

Todos los `csproj` y, potencialmente, todo archivo `.cs`. Estimación por el
tamaño actual (~5.650 líneas en `src/` + `tools/`): trabajo mecánico
concentrado en `Raftel.Infrastructure` (el proyecto con más interacción con EF).

Añadir al final en `Directory.Build.props` para que aplique a proyectos nuevos:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

> Nota: hoy **no existe** `Directory.Build.props` en la raíz (solo
> `Directory.Packages.props`). Crearlo es parte de esta feature y sirve para
> centralizar `TargetFramework`, `ImplicitUsings` y `LangVersion`, hoy repetidos
> en los diez `csproj`.

## 4. Plan de implementación

Por cada proyecto de la lista §2.1:

1. Activar `<Nullable>enable</Nullable>` con `WarningsNotAsErrors` temporal.
2. `dotnet build` y contar avisos por código (`CS86xx`).
3. Corregir por categoría, empezando por CS8618 (campos no inicializados) que
   suele arrastrar el resto.
4. Revisar cada `!` introducido; sustituir por comprobación real donde se pueda.
5. Quitar `WarningsNotAsErrors`, añadir `TreatWarningsAsErrors`.
6. `dotnet test` completo — la nulabilidad puede cambiar comportamiento en
   serialización JSON y en el binding de `QueryEndpointMapper`.
7. Commit por proyecto.

Al final:

8. Crear `Directory.Build.props` con la configuración común.
9. Limpiar de los `csproj` las propiedades ya centralizadas.
10. Documentar en `BREAKING_CHANGES.md`.

## 5. Tests

- La suite existente (258 tests) es la red de seguridad; debe pasar tras cada
  proyecto.
- Atención especial a `QueryEndpointMapper.BuildRequestFromRouteAndQuery`, que
  construye requests por reflexión con `ConvertSimpleType`: las anotaciones
  cambian qué se considera parámetro opcional.
- Test nuevo: un proyecto de prueba con `nullable enable` que consume
  `Raftel.Domain` y comprueba que el análisis fluye (por ejemplo, que
  `GetByIdAsync` obliga a comprobar null).

## 6. Criterios de aceptación

- [ ] Ningún `csproj` de `src/`, `tools/` ni `demo/` con `Nullable disable`.
- [ ] `TreatWarningsAsErrors` activo en `src/`.
- [ ] Ningún `#nullable disable` en el código.
- [ ] Los `!` restantes están justificados con comentario.
- [ ] `Directory.Build.props` centraliza la configuración común.
- [ ] Los 258 tests siguen en verde.

## 7. Alternativas descartadas

- **Migrar toda la solución en un solo PR**: cientos de avisos a la vez,
  imposible de revisar, alta probabilidad de silenciar con `!`.
- **`<Nullable>annotations</Nullable>`** (anotar sin avisar): da información al
  consumidor pero no detecta bugs propios. Paso intermedio válido si la migración
  completa se atasca.
- **Dejarlo como está**: cada feature nueva del backlog añade código sin red de
  nulabilidad; el coste de migrar solo crece.

## 8. Breaking changes

Las anotaciones cambian la superficie pública: un consumidor con
`TreatWarningsAsErrors` puede empezar a ver errores donde antes no los había.
Es el objetivo de la feature, pero hay que anunciarlo como cambio mayor.
