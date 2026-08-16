# 13 — Rate limiting, CORS y cabeceras de seguridad

> Estado: **Pendiente**
> Ámbito: `Raftel.Api.Server`
> Rompe API pública: no (aditivo, opt-in)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [04](04-logging-estructurado.md)

## 1. Motivación

`Raftel.Api.Server` no configura CORS, ni rate limiting, ni cabeceras de
seguridad. El framework expone un endpoint de login
(`AuthorizationController`, `/connect/token`) con password flow **sin ninguna
limitación de intentos**: fuerza bruta libre contra credenciales de usuario.

Para un framework que se presenta como base de APIs, esto no puede quedar como
tarea del consumidor: es la clase de cosa que se olvida.

## 2. Diseño

Todo sobre APIs nativas de .NET 10 (`Microsoft.AspNetCore.RateLimiting`), sin
dependencias externas.

### 2.1 Rate limiting

Tres políticas nombradas listas para usar:

| Política | Algoritmo | Por defecto | Uso |
|---|---|---|---|
| `raftel-global` | token bucket | 100 req/min por IP | Todos los endpoints |
| `raftel-auth` | fixed window | 5 intentos / 5 min por IP+usuario | `/connect/token`, registro, reset de contraseña |
| `raftel-write` | sliding window | 30 req/min por usuario | Comandos (`POST`/`PUT`/`DELETE`) |

Partición de la clave: usuario autenticado si existe, si no la IP. En
multitenancy, incluir el tenant para que un tenant ruidoso no consuma la cuota
de otro.

```csharp
RateLimitPartition.GetTokenBucketLimiter(
    partitionKey: currentUser.Id?.ToString() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
    factory: _ => new TokenBucketRateLimiterOptions { ... });
```

> **IP real tras proxy**: `RemoteIpAddress` es la del balanceador si no se
> configura `ForwardedHeadersOptions` con `KnownProxies`/`KnownNetworks`. Sin
> eso, todo el tráfico comparte partición y el límite es inútil (o bloquea a
> todos a la vez). Se incluye la configuración y se **documenta como requisito**;
> confiar en `X-Forwarded-For` sin lista de proxies conocidos permite evadir el
> límite falsificando la cabecera.

Respuesta al superar el límite: `429 Too Many Requests` con `ProblemDetails`
(consistente con [01](01-error-tipado-y-mapeo-http.md)) y cabecera `Retry-After`.

El límite de autenticación se registra en el log en `Warning` — señal de ataque
de fuerza bruta.

### 2.2 CORS

Nada de `AllowAnyOrigin()` por defecto. Configuración explícita:

```csharp
public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH"];
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization", "X-Correlation-Id"];
    public string[] ExposedHeaders { get; set; } = ["X-Correlation-Id", "X-Total-Count"];
    public bool AllowCredentials { get; set; } = false;
    public TimeSpan PreflightMaxAge { get; set; } = TimeSpan.FromHours(1);
}
```

Reglas duras, validadas al arrancar:

- `AllowCredentials = true` junto a `AllowedOrigins` conteniendo `"*"` ⇒
  **excepción al arrancar**. Es una combinación prohibida por la especificación y
  un agujero clásico.
- Lista de orígenes vacía ⇒ CORS deshabilitado (no "todo permitido").
- En Development se permite relajar con `AllowDevelopmentOrigins = true`,
  registrando un aviso en el log.

`ExposedHeaders` debe incluir las cabeceras que introduce el framework
(`X-Correlation-Id` de [04](04-logging-estructurado.md), `X-Total-Count` de
[03](03-paginacion-y-ordenacion.md)); si no, el navegador no las deja leer.

### 2.3 Cabeceras de seguridad

Middleware `SecurityHeadersMiddleware`:

| Cabecera | Valor por defecto |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` (API pura) |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (solo HTTPS, solo fuera de Development) |
| `X-Powered-By` / `Server` | eliminadas |

CSP `default-src 'none'` es correcto para una API JSON. Si el consumidor sirve
Swagger UI, necesita relajarlo para esa ruta: opción
`SwaggerCspOverride` aplicada solo al path de la documentación.

### 2.4 Registro

```csharp
app.UseRaftelSecurity();   // forwarded headers → security headers → CORS → rate limiting
```

El orden importa y por eso se ofrece una sola extensión: forwarded headers debe
ir **antes** que el rate limiting (necesita la IP real), y CORS antes de la
autorización.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Api.Server/Security/RateLimitingExtensions.cs`
- `src/Raftel.Api.Server/Security/RateLimitOptions.cs`
- `src/Raftel.Api.Server/Security/CorsOptions.cs` + `CorsExtensions.cs`
- `src/Raftel.Api.Server/Middlewares/SecurityHeadersMiddleware.cs`
- `src/Raftel.Api.Server/Security/SecurityHeadersOptions.cs`
- `src/Raftel.Api.Server/Security/RaftelSecurityExtensions.cs`
- `tests/Raftel.Api.FunctionalTests/Security/{RateLimitingTests,CorsTests,SecurityHeadersTests}.cs`

**Modificados**
- `src/Raftel.Api.Server/Features/Users/AuthorizationController.cs` — aplicar `raftel-auth`
- `src/Raftel.Api.Server/AutoEndpoints/CommandEndpointMapper.cs` — aplicar `raftel-write`
- `demo/**/Program.cs`, `appsettings.json`

## 4. Plan de implementación

1. **(test funcional)** 6 intentos de login fallidos seguidos ⇒ el sexto responde
   `429` con `Retry-After`.
2. Implementar las políticas de rate limiting y aplicarlas al endpoint de token.
3. **(test)** El límite se particiona por IP: dos IPs distintas no se afectan.
4. **(test)** Configuración con `AllowCredentials = true` y origen `*` ⇒ el
   arranque falla con mensaje claro.
5. Implementar `CorsOptions` con validación de arranque.
6. **(test funcional)** Preflight desde origen permitido ⇒ 204 con cabeceras;
   desde origen no permitido ⇒ sin cabeceras CORS.
7. **(test funcional)** Toda respuesta lleva las cabeceras de seguridad; ninguna
   lleva `Server` ni `X-Powered-By`.
8. Implementar `SecurityHeadersMiddleware`.
9. Forwarded headers + documentación del despliegue tras proxy.
10. Extensión agregada `UseRaftelSecurity` y orden documentado.

## 5. Tests

- **Functional**: los tres bloques, incluyendo el caso de evasión por
  `X-Forwarded-For` falsificado sin proxies conocidos.
- **Unit**: validación de opciones al arrancar.
- **Functional**: `X-Correlation-Id` y `X-Total-Count` legibles desde el navegador
  (presentes en `Access-Control-Expose-Headers`).

## 6. Criterios de aceptación

- [ ] El endpoint de token está limitado por defecto, sin configuración adicional.
- [ ] Ninguna configuración por defecto permite cualquier origen.
- [ ] `AllowCredentials` + `*` aborta el arranque.
- [ ] Todas las respuestas llevan las cabeceras de seguridad.
- [ ] Documentado el requisito de forwarded headers tras proxy.
- [ ] Los límites superados se registran en el log.

## 7. Alternativas descartadas

- **Rate limiting en el ingress/API gateway únicamente**: correcto en producción
  madura, pero deja desprotegido el desarrollo local, los despliegues simples y
  a quien no tenga gateway. Defensa en profundidad: ambos.
- **AspNetCoreRateLimit (paquete)**: .NET trae rate limiting nativo desde .NET 7;
  no hace falta dependencia externa.
- **Bloqueo de cuenta tras N intentos** en lugar de rate limiting: abre la puerta
  a denegación de servicio contra usuarios concretos. ASP.NET Identity ya ofrece
  lockout configurable; se documenta como complemento, no sustituto.
