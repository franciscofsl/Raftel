# Plan de implementación: optimización de los tests de integración (Testcontainers)

> **Estado: Fases 0–4 implementadas.** `Raftel.Infrastructure.Tests`, `Raftel.Application.IntegrationTests`
> y `Raftel.Api.FunctionalTests` ya comparten contenedor por assembly (vía `ICollectionFixture`) y usan
> Respawn para aislar datos entre tests donde aplica. Suite completa verde. Pendientes: Fase 5
> (paralelismo/límites de recursos) y Fase 6 final (medición comparativa formal con `docker stats`).

## 1. Diagnóstico (causa raíz)

El consumo de memoria venía de que **se creaba un contenedor de base de datos por cada
método de test**, no por clase ni por suite.

En xUnit se instancia una clase de test nueva **por cada `[Fact]`/`[Theory]`**. Las bases
originales creaban el contenedor en el constructor + `IAsyncLifetime` a nivel de clase:

| Base / fixture | Ámbito real (antes) | Contenedor por… | Imagen |
|---|---|---|---|
| `IntegrationTestBase` | `IAsyncLifetime` de la clase de test | **método de test** | SQL Server 2022 |
| `InfrastructureTestBase` | `IAsyncLifetime` de la clase de test | **método de test** | SQL Server 2022 |
| `PostgreSqlInfrastructureTestBase` | `IAsyncLifetime` de la clase de test | **método de test** | Postgres 17 |
| `DatabaseProviderTests` | contenedor inline dentro de cada `[Fact]` | **método de test** | SQL Server + Postgres |
| `ApiTestFactory` | `IClassFixture` | clase de test (5 clases) | SQL Server 2022 |

Con paralelismo de xUnit, decenas de contenedores SQL Server (~1,5–2 GB de RAM cada uno) y
Postgres se levantaban a la vez. Ese era el origen del problema.

## 2. Estrategia

1. **Compartir el contenedor** a nivel de assembly con `ICollectionFixture`. Un solo SQL
   Server y un solo Postgres por proyecto de tests, arrancados una vez.
2. **Aislar los datos entre tests** sin recrear el contenedor. El esquema se crea una vez
   (`EnsureCreated`) y entre tests se limpian los datos con **Respawn**.

## 3. Implementación realizada

### `Raftel.Infrastructure.Tests`
- `SqlServerTestCollection` / `PostgreSqlTestCollection` (`[CollectionDefinition]` +
  `ICollectionFixture<...Fixture>`): un contenedor SQL Server y uno PostgreSQL para todo el
  assembly.
- `InfrastructureTestBase` / `PostgreSqlInfrastructureTestBase`: ya no instancian ni paran el
  contenedor; lo reciben por constructor y, en `InitializeAsync`, ejecutan `Respawner.ResetAsync`
  (instancia estática, creada una vez, reutilizada en todos los tests de esa base).
- Las ~9 clases de test concretas ahora declaran `[Collection(...)]` y reenvían el fixture
  inyectado al constructor base.
- `DatabaseProviderTests` se dividió en `PostgreSqlDatabaseProviderTests` y
  `SqlServerDatabaseProviderTests`, cada una en su colección, reutilizando el contenedor
  compartido en lugar de crear uno inline por `[Fact]`.
- `UserAuditingTests.AddAsync_WithoutCurrentUser_...` (que creaba un contenedor extra inline)
  ahora reutiliza el contenedor de la colección, solo construye un `ServiceProvider` nuevo.

### `Raftel.Application.IntegrationTests`
- `IntegrationSqlServerTestCollection` propia (las definiciones de colección son por
  assembly, no se pueden compartir físicamente entre proyectos/procesos aunque reutilicen el
  mismo tipo `SqlServerTestContainerFixture` vía referencia de proyecto).
- `IntegrationTestBase` sigue el mismo patrón: recibe el fixture, resetea con Respawn.

### `Raftel.Api.FunctionalTests`
- `ApiTestCollection` (`ICollectionFixture<ApiTestFactory>`): las 5 clases funcionales pasan
  de `IClassFixture<ApiTestFactory>` (1 contenedor por clase) a compartir un único
  `ApiTestFactory`/contenedor para todo el assembly.
- Sin Respawn aquí: el seed de `SeedData` (rol admin, cliente OpenIddict) debe sobrevivir
  entre tests, y los tests ya usan datos únicos (GUIDs en emails, nombres específicos usados
  una sola vez), así que no hay colisión de datos al compartir contenedor.

### Paquetes
- `Respawn` 7.0.0 añadido a `Directory.Packages.props` y a los csproj de
  `Raftel.Infrastructure.Tests` y `Raftel.Application.IntegrationTests`.
- `Microsoft.Data.SqlClient` central actualizado de 5.1.6 → 6.1.1 (versión mínima exigida
  transitivamente por `Microsoft.EntityFrameworkCore.SqlServer` 10.0.1; sin esto, NuGet
  fallaba con NU1605 por "degradación de paquete").

## 4. Resultado verificado

- `dotnet test` en `Raftel.Infrastructure.Tests`: 69/69 ✅, pico observado de **2 contenedores**
  (1 SQL Server + 1 Postgres) en vez de uno por test.
- `dotnet test` en `Raftel.Application.IntegrationTests`: 6/6 ✅, 1 contenedor SQL Server
  compartido por las 2 clases.
- `dotnet test` en `Raftel.Api.FunctionalTests`: 13/13 ✅, 1 contenedor SQL Server compartido
  por las 5 clases.
- `dotnet test Raftel.sln` (suite completa): todos los proyectos en verde.
- Sin contenedores huérfanos al finalizar (Ryuk de Testcontainers limpia y `StopAsync` se
  ejecuta una vez al cerrar la colección).

## 5. Pendiente (no implementado en esta iteración)

- **Fase 5 — Paralelismo y límites de recursos**: configurar `xunit.runner.json` con
  `maxParallelThreads` si se quiere acotar cuántos *assemblies* corren en paralelo cuando se
  ejecuta `dotnet test` a nivel de solución (cada assembly sigue siendo un proceso/testhost
  independiente con sus propios 1-2 contenedores). Evaluar límites de memoria del contenedor
  (`--memory`) si aún se nota presión en CI.
- **Fase 6 — Medición comparativa formal**: capturar `docker stats` antes/después con un
  script reproducible para documentar la mejora cuantitativa (esta iteración verificó
  manualmente el pico de contenedores con `docker ps`, pero no dejó un script ni números de
  memoria pico formales).

## 6. Riesgos y notas para quien continúe

- **Estática por proceso**: `_respawner` es un campo `static` en cada base (`InfrastructureTestBase`,
  `PostgreSqlInfrastructureTestBase`, `IntegrationTestBase`). Funciona porque xUnit ejecuta en
  serie las clases de una misma `[Collection]`; si se cambia el comportamiento de paralelismo
  por defecto, revisar esta suposición.
- **`ApiTestFactory` sin reset**: si en el futuro se añaden tests que dependan de un estado
  limpio (conteos exactos de filas, etc.), habrá que introducir Respawn ahí también y revisar
  qué tablas (rol admin, cliente OpenIddict `web-app`) no se deben borrar.
- **Nombres de colección duplicados entre proyectos**: `Raftel.Infrastructure.Tests` tiene
  `SqlServerTestCollection` y `Raftel.Application.IntegrationTests` tiene
  `IntegrationSqlServerTestCollection` (nombre distinto a propósito) para evitar ambigüedad
  cuando un archivo de test importa ambos namespaces.
