# 18 — Paquete `Raftel.Testing`

> Estado: **Pendiente**
> Ámbito: proyecto nuevo `src/Raftel.Testing`
> Rompe API pública: no (aditivo)
> Depende de: la mayoría de features anteriores (empaqueta lo que exista)

## 1. Motivación

El repositorio tiene una infraestructura de test buena (258 tests, Testcontainers,
Respawn, fixtures de integración y funcionales), pero **vive en `tests/` y no se
publica**. Quien consuma Raftel desde NuGet tiene que reconstruirla desde cero:
fixture de base de datos, `WebApplicationFactory`, autenticación falsa,
sustitución de `TimeProvider`, limpieza entre tests.

Un framework que impone patrones (Result, CQRS, multitenancy, permisos) debe dar
las herramientas para testear aplicaciones que los usan. Si no, el consumidor
prueba mal o no prueba.

## 2. Diseño

Proyecto nuevo `src/Raftel.Testing`, publicado como paquete independiente. Toma
como base lo ya escrito en `tests/**` y lo generaliza.

### 2.1 Builders de dominio

```csharp
public abstract class EntityBuilder<TEntity, TBuilder> where TBuilder : EntityBuilder<TEntity, TBuilder>
{
    public abstract TEntity Build();
    public static implicit operator TEntity(EntityBuilder<TEntity, TBuilder> builder) => builder.Build();
}
```

Con builders listos para los agregados del framework:
`UserBuilder`, `TenantBuilder`, `RoleBuilder`.

```csharp
var user = new UserBuilder().WithEmail("a@b.com").WithRole(role).Build();
```

### 2.2 Aserciones sobre `Result`

Lo que hoy se repite en cada test:

```csharp
result.ShouldBeSuccess();
result.ShouldBeFailure();
result.ShouldBeFailureWith("Users.NotFound");
result.ShouldBeFailureOfType(ErrorType.NotFound);
result.ShouldBeSuccessWithValue(expected);
```

Extensiones sobre `Result`/`Result<T>` compatibles con Shouldly (y con mensajes
de error útiles: mostrar el `Error.Code` real cuando falla la aserción).

### 2.3 Dobles de las abstracciones del framework

```csharp
public sealed class FakeCurrentUser : ICurrentUser
{
    public static FakeCurrentUser WithPermissions(params string[] permissions);
    public static FakeCurrentUser Anonymous();
}

public sealed class FakeCurrentTenant : ICurrentTenant { ... }
public sealed class FakeAuditLogScope : IAuditLogScope { ... }
public sealed class CollectingDomainEventsDispatcher : IDomainEventsDispatcher
{
    public IReadOnlyList<IDomainEvent> Dispatched { get; }   // aserciones sobre eventos
}
```

`TimeProvider` ya está registrado en el contenedor
(`services.AddSingleton(TimeProvider.System)`), así que basta con exponer un
`FakeTimeProvider` y documentar cómo sustituirlo — el framework ya está
preparado para tiempo controlable, hay que aprovecharlo.

### 2.4 Fixture de integración

Generalización de lo que hay en `tests/Raftel.Application.IntegrationTests`:

```csharp
public abstract class RaftelIntegrationTest<TDbContext> : IAsyncLifetime
    where TDbContext : RaftelDbContext<TDbContext>
{
    protected IServiceProvider Services { get; }
    protected ICommandDispatcher Commands { get; }
    protected IQueryDispatcher Queries { get; }

    protected Task<Result> SendAsync(ICommand command);
    protected Task ResetDatabaseAsync();          // Respawn
    protected Task AsUserAsync(params string[] permissions);
    protected Task AsTenantAsync(TenantId tenantId);
}
```

Contenedor compartido entre tests de la misma colección (arrancar un contenedor
por test es inviable). Respawn para limpiar entre tests en vez de recrear.

Soporte para SqlServer y PostgreSQL con el mismo fixture, parametrizado — hoy el
framework soporta ambos y los tests deberían cubrir ambos.

### 2.5 Fixture funcional

```csharp
public abstract class RaftelApiTest<TEntryPoint> : IAsyncLifetime where TEntryPoint : class
{
    protected HttpClient Client { get; }
    protected HttpClient AuthenticatedClient(params string[] permissions);
    protected Task<TResponse> GetAsync<TResponse>(string url);
    protected Task<ProblemDetails> GetProblemAsync(string url);
}
```

Autenticación de test mediante un esquema falso registrado solo en el host de
pruebas — **nunca** compilado en el ensamblado de producción. Se documenta con
énfasis: un esquema de autenticación de bypass filtrado a producción es un
agujero total.

### 2.6 Aserciones de arquitectura

`tests/Raftel.ArchitectureTests` usa NetArchTest para las reglas del proyecto.
Se empaquetan como reglas reutilizables:

```csharp
RaftelArchitectureRules.For(typeof(Program).Assembly)
    .DomainHasNoDependencies()
    .ApplicationDependsOnlyOnDomain()
    .NoEntityFrameworkInDomainOrApi()
    .HandlersAreSealed()
    .CommandsAreRecords()
    .AssertAll();
```

Así el consumidor hereda las mismas garantías arquitectónicas en **su** solución.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Testing/Raftel.Testing.csproj`
- `src/Raftel.Testing/Assertions/ResultAssertions.cs`
- `src/Raftel.Testing/Builders/{EntityBuilder,UserBuilder,TenantBuilder,RoleBuilder}.cs`
- `src/Raftel.Testing/Fakes/{FakeCurrentUser,FakeCurrentTenant,CollectingDomainEventsDispatcher}.cs`
- `src/Raftel.Testing/Integration/RaftelIntegrationTest.cs`
- `src/Raftel.Testing/Integration/DatabaseFixture.cs`
- `src/Raftel.Testing/Functional/RaftelApiTest.cs`
- `src/Raftel.Testing/Functional/TestAuthenticationHandler.cs`
- `src/Raftel.Testing/Architecture/RaftelArchitectureRules.cs`
- `tests/Raftel.Testing.Tests/**`

**Modificados**
- `Raftel.sln`
- `tests/**` — migrar los tests existentes a usar el paquete (valida el diseño y
  elimina duplicación)
- `.github/workflows/release.yml` — el bucle ya empaqueta todo `src/**`, así que
  entra automáticamente; verificar

## 4. Plan de implementación

1. Crear el proyecto y las aserciones de `Result`.
2. Migrar un test existente a las aserciones nuevas; comprobar que los mensajes
   de fallo son legibles.
3. Fakes de `ICurrentUser`, `ICurrentTenant`, dispatcher recolector.
4. Migrar los tests unitarios de aplicación a los fakes; borrar duplicación.
5. Extraer `DatabaseFixture` de `tests/Raftel.Application.IntegrationTests`
   generalizando el `TDbContext`.
6. Migrar los tests de integración; verificar que el tiempo total no empeora.
7. Extraer `RaftelApiTest` de `tests/Raftel.Api.FunctionalTests`.
8. **(test)** El handler de autenticación de test **no** existe fuera del
   ensamblado de testing (test de arquitectura).
9. Empaquetar las reglas de arquitectura desde `tests/Raftel.ArchitectureTests`.
10. Documentar con ejemplos completos en `docs/`.

## 5. Tests

El paquete de testing necesita sus propios tests (`tests/Raftel.Testing.Tests`):
una aserción que no falla cuando debería es peor que no tenerla.

- Cada aserción falla cuando debe y con un mensaje útil.
- El fixture limpia realmente entre tests (un test escribe, el siguiente ve la
  base vacía).
- La migración de los 258 tests existentes es en sí la validación del diseño.

## 6. Criterios de aceptación

- [ ] Los tests actuales del repositorio usan el paquete; duplicación eliminada.
- [ ] El tiempo total de la suite no empeora.
- [ ] El esquema de autenticación de test no puede acabar en producción.
- [ ] El fixture soporta SqlServer y PostgreSQL.
- [ ] Documentación con ejemplos ejecutables.

## 7. Alternativas descartadas

- **Dejar los helpers en `tests/`**: es lo actual; el consumidor no los recibe.
- **Un paquete por tipo de test** (`Raftel.Testing.Unit`, `.Integration`,
  `.Functional`): fragmentación innecesaria para el tamaño del proyecto. Se puede
  dividir más adelante si las dependencias pesan (Testcontainers no debería
  arrastrarse a un test unitario — valorar `Raftel.Testing` +
  `Raftel.Testing.Integration` si eso molesta).
- **Depender de una librería de aserciones concreta**: se elige Shouldly por
  coherencia con el repositorio, pero las aserciones se implementan de forma que
  no impidan usar otra.
