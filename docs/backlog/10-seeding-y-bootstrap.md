# 10 — Seeding y bootstrap de datos iniciales

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo)
> Depende de: [05](05-transacciones-explicitas.md), [09](09-health-checks.md)

## 1. Motivación

El framework trae un modelo de autorización (`Role`, `Permission`,
`PermissionCollection`, `UsersPermissions`, `TenantsPermissions`) pero **no hay
nada que lo inicialice**. Al arrancar contra una base vacía:

- No existe ningún rol.
- No existe ningún permiso persistido, aunque las constantes estén declaradas en
  código (`UsersPermissions`, `TenantsPermissions`).
- No hay usuario administrador, así que no hay forma de asignar permisos vía API
  — problema del huevo y la gallina.
- No hay tenant por defecto en escenarios single-tenant.

Cada consumidor del framework acaba escribiendo su propio script SQL ad hoc.
Tampoco hay ejecución automática de migraciones.

## 2. Diseño

### 2.1 `IDataSeeder`

```csharp
// Raftel.Application/Abstractions/Seeding/IDataSeeder.cs
public interface IDataSeeder
{
    int Order { get; }                    // menor primero
    string Name { get; }
    Task<Result> SeedAsync(CancellationToken cancellationToken = default);
}
```

Autodescubrimiento por ensamblado, igual que handlers y validadores (reutilizar
el escaneo de `DependencyInjection.RegisterHandlers`).

### 2.2 Idempotencia

Regla dura: **todo seeder debe poder ejecutarse N veces sin efecto adicional.**
Se comprueba de dos formas complementarias:

1. El seeder consulta antes de insertar (`if (await repository.ExistsAsync(...)) return Result.Success();`).
2. Registro de ejecuciones en tabla `SeedHistory(Name, AppliedOn, Version)`. Un
   seeder con `RunOnce = true` no se reejecuta nunca; uno idempotente sí puede
   reejecutarse para reconciliar.

Los seeders de **permisos** deben ser reconciliadores, no `RunOnce`: al añadir un
permiso nuevo al código hay que persistirlo en una base existente.

### 2.3 Seeders incluidos

- **`PermissionsSeeder`** (Order 100): descubre por reflexión todas las
  constantes `string` de las clases marcadas como colección de permisos
  (`UsersPermissions`, `TenantsPermissions` y las del consumidor) y las
  sincroniza con la tabla `Permissions`. Los permisos que desaparecen del código
  **no se borran** por defecto (podrían estar asignados); se marcan como
  obsoletos y se reporta en el log.
- **`DefaultRolesSeeder`** (Order 200): crea los roles configurados y les asigna
  permisos.
- **`AdminUserSeeder`** (Order 300): crea el usuario administrador inicial.
- **`DefaultTenantSeeder`** (Order 50): tenant por defecto en modo single-tenant.

### 2.4 Credenciales del administrador

Punto crítico de seguridad. Reglas:

1. **Nunca** una contraseña por defecto en código ni en `appsettings.json` del
   repositorio.
2. La contraseña se lee de configuración (`Raftel:Seed:AdminPassword`), que en
   producción debe venir de variable de entorno, user-secrets o key vault.
3. Si no se ha proporcionado y el entorno **no** es Development, el seeder
   **falla el arranque** con un mensaje claro. No genera una contraseña
   aleatoria silenciosa ni deja al usuario sin contraseña.
4. En Development se puede generar una aleatoria y **escribirla en el log** una
   sola vez, marcado explícitamente como solo-desarrollo.
5. El usuario administrador se crea con `MustChangePassword = true` si el
   proveedor de identidad lo soporta.

### 2.5 Ejecución

```csharp
public sealed class SeedRunner(IEnumerable<IDataSeeder> seeders, IUnitOfWork unitOfWork, ...)
{
    public async Task<Result> RunAsync(CancellationToken cancellationToken = default);
}
```

Cada seeder corre en **su propia transacción** ([05](05-transacciones-explicitas.md)):
un fallo en el seeder 300 no revierte los seeders 100 y 200 ya aplicados.

Punto de entrada:

```csharp
await app.MigrateAndSeedAsync();     // extensión sobre IHost
```

Con opciones:

```csharp
public sealed class SeedOptions
{
    public bool ApplyMigrationsOnStartup { get; set; } = false;   // false en producción
    public bool RunSeedersOnStartup { get; set; } = true;
    public bool FailFastOnError { get; set; } = true;
}
```

`ApplyMigrationsOnStartup = false` por defecto: aplicar migraciones desde la app
en un despliegue con varias réplicas provoca carreras. La recomendación
documentada es un job de migración separado; la opción existe para desarrollo y
despliegues de instancia única.

### 2.6 Multitenancy

Un seeder puede ser global o por tenant:

```csharp
public interface ITenantDataSeeder : IDataSeeder
{
    Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
```

El runner recorre los tenants activos fijando `ICurrentTenant` en cada iteración.
Al crear un tenant nuevo (`CreateTenantCommandHandler`) se disparan sus seeders
— vía evento de dominio, encaja con [07](07-outbox-domain-events.md).

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Abstractions/Seeding/{IDataSeeder,ITenantDataSeeder,ISeedRunner}.cs`
- `src/Raftel.Application/SeedOptions.cs`
- `src/Raftel.Infrastructure/Seeding/SeedRunner.cs`
- `src/Raftel.Infrastructure/Seeding/SeedHistory.cs` + configuración EF
- `src/Raftel.Infrastructure/Seeding/Seeders/{Permissions,DefaultRoles,AdminUser,DefaultTenant}Seeder.cs`
- `src/Raftel.Infrastructure/Seeding/HostSeedingExtensions.cs`
- `tests/Raftel.Infrastructure.Tests/Seeding/**`
- `tests/Raftel.Application.IntegrationTests/SeedingTests.cs`

**Modificados**
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `src/Raftel.Application/DependencyInjection.cs` — escaneo de seeders
- `demo/**/Program.cs`
- `docs/getting-started.md` (hoy vacío — buen momento para escribirlo)

## 4. Plan de implementación

1. **(test)** `SeedRunner` ejecuta en orden ascendente de `Order`.
2. **(test)** Un seeder que falla no impide los anteriores ya aplicados y, con
   `FailFastOnError = true`, detiene los siguientes.
3. Implementar `SeedRunner` + `SeedHistory`.
4. **(test integración)** `PermissionsSeeder` sobre base vacía inserta todos los
   permisos; segunda ejecución no inserta duplicados; permiso nuevo en código se
   añade sin tocar los existentes.
5. Implementar `PermissionsSeeder`.
6. **(test)** `AdminUserSeeder` **falla** en entorno Production sin contraseña
   configurada. Test explícito de seguridad.
7. Implementar `AdminUserSeeder` y `DefaultRolesSeeder`.
8. **(test integración)** `ITenantDataSeeder` corre una vez por tenant con el
   `ICurrentTenant` correcto.
9. `MigrateAndSeedAsync` + integración con health checks (no "ready" hasta
   terminar el seeding).
10. Escribir `docs/getting-started.md` con el flujo de arranque completo.

## 5. Tests

- **Unit**: orden, aislamiento de fallos, historial.
- **Integration**: idempotencia (ejecutar dos veces y comparar el estado),
  reconciliación de permisos, seeding por tenant.
- **Seguridad**: sin contraseña en Production ⇒ arranque abortado.

## 6. Criterios de aceptación

- [ ] Base vacía + arranque ⇒ sistema utilizable (permisos, rol admin, usuario admin).
- [ ] Ejecutar el seeding dos veces deja el mismo estado.
- [ ] Ninguna contraseña por defecto en el repositorio.
- [ ] Un permiso nuevo en código aparece en una base existente sin intervención manual.
- [ ] La app no se marca "ready" hasta que el seeding termina.

## 7. Alternativas descartadas

- **`HasData` de EF Core**: los datos van dentro de la migración, no se pueden
  calcular en tiempo de ejecución (imposible reconciliar permisos por reflexión)
  y complican mucho el diff de migraciones.
- **Scripts SQL sueltos**: no portables entre SqlServer y PostgreSQL, fuera del
  alcance de los tests.
- **Endpoint de bootstrap** (`POST /setup`): superficie de ataque abierta al
  arrancar; una carrera y cualquiera crea el admin.
