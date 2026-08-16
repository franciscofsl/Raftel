# 21 — Flujos de autenticación adicionales

> Estado: **Pendiente**
> Ámbito: `Raftel.Infrastructure`, `Raftel.Api.Server`
> Rompe API pública: no (aditivo)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [13](13-rate-limiting-cors-security-headers.md)

## 1. Motivación

La configuración actual de OpenIddict habilita **solo dos flujos**:

```csharp
opt.SetTokenEndpointUris("/connect/token")
    .AllowPasswordFlow()
    .AllowRefreshTokenFlow();
```

El **password flow (ROPC) está desaconsejado** por OAuth 2.1 y por las buenas
prácticas de seguridad de la IETF: obliga a que la aplicación cliente maneje las
credenciales del usuario en claro, impide MFA y federación, y no sirve para
clientes públicos (SPA, móvil).

Faltan además:

- **Authorization Code + PKCE**: el flujo recomendado para SPAs y apps móviles.
- **Client Credentials**: para integraciones máquina-a-máquina.
- **API keys**: acceso programático simple, muy pedido en APIs de producto.
- Endpoints de introspección, revocación, userinfo, logout y descubrimiento
  (`/.well-known/openid-configuration`).

Otro punto: los certificados son de desarrollo, **sin alternativa configurada**:

```csharp
opt.AddDevelopmentEncryptionCertificate()
   .AddDevelopmentSigningCertificate();
```

Esto en producción es un fallo grave — los certificados de desarrollo son
efímeros y no compartidos entre instancias, así que los tokens emitidos por una
réplica no los valida otra.

## 2. Diseño

### 2.1 Flujos configurables

```csharp
public sealed class AuthenticationOptions
{
    public bool EnablePasswordFlow { get; set; } = false;        // desactivado por defecto
    public bool EnableAuthorizationCodeFlow { get; set; } = true;
    public bool EnableClientCredentialsFlow { get; set; } = false;
    public bool EnableRefreshTokenFlow { get; set; } = true;
    public bool EnableDeviceFlow { get; set; } = false;
    public bool RequirePkce { get; set; } = true;                // obligatorio si hay authorization code
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);
    public bool UseReferenceTokens { get; set; } = false;
}
```

Cambio de valor por defecto respecto a hoy: **password flow desactivado**, con
un aviso en el log al activarlo explicando por qué se desaconseja. Es breaking
para quien lo use, así que se documenta y se ofrece la opción.

`RequirePkce = true` no negociable cuando hay clientes públicos: sin PKCE, el
authorization code es interceptable.

### 2.2 Endpoints estándar

```csharp
opt.SetAuthorizationEndpointUris("/connect/authorize")
   .SetTokenEndpointUris("/connect/token")
   .SetUserInfoEndpointUris("/connect/userinfo")
   .SetIntrospectionEndpointUris("/connect/introspect")
   .SetRevocationEndpointUris("/connect/revoke")
   .SetEndSessionEndpointUris("/connect/logout");
```

Con el descubrimiento OIDC que OpenIddict expone automáticamente.

### 2.3 Rotación de refresh tokens

Activar rotación con detección de reutilización: si llega un refresh token ya
consumido, se revoca **toda la cadena** de tokens de esa sesión (indicio de robo
de token). OpenIddict lo soporta; hay que configurarlo y testearlo.

### 2.4 Certificados en producción

Configuración explícita, con **fallo al arrancar** si el entorno no es
Development y no hay certificado configurado:

```csharp
public sealed class CertificateOptions
{
    public string? SigningCertificateThumbprint { get; set; }
    public string? EncryptionCertificateThumbprint { get; set; }
    public string? SigningCertificatePath { get; set; }        // + password desde secretos
    public CertificateStoreLocation StoreLocation { get; set; }
}
```

Nunca una contraseña de certificado en `appsettings.json` del repositorio.

### 2.5 API keys

Esquema de autenticación propio, complementario a OAuth:

```
Authorization: ApiKey rk_live_<id>_<secret>
```

Modelo:

```csharp
public sealed class ApiKey : AggregateRoot<ApiKeyId>
{
    public string Name { get; private set; }
    public string Prefix { get; private set; }          // visible, para identificar la clave
    public string SecretHash { get; private set; }      // hash, nunca el secreto
    public DateTimeOffset? ExpiresOn { get; private set; }
    public DateTimeOffset? RevokedOn { get; private set; }
    public DateTimeOffset? LastUsedOn { get; private set; }
    public PermissionCollection Permissions { get; private set; }
    public TenantId? TenantId { get; private set; }
}
```

Reglas de seguridad:

- El secreto se genera con `RandomNumberGenerator` (≥256 bits) y se muestra
  **una sola vez**, al crearlo. Nunca recuperable después.
- Se almacena **hasheado** (PBKDF2/Argon2 con salt por clave; no un hash rápido).
- Comparación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).
- El `Prefix` permite localizar la clave sin hashear todo el almacén en cada
  petición: se busca por prefijo y se verifica el hash de la candidata.
- Expiración obligatoria configurable (`MaxLifetime`), revocación inmediata.
- Rate limiting específico ([13](13-rate-limiting-cors-security-headers.md)).
- `LastUsedOn` actualizado de forma **asíncrona y throttled** (no una escritura
  por petición).
- Los permisos de la clave son un **subconjunto** de los de su creador: una API
  key no puede escalar privilegios.

Las claves se integran con `ICurrentUser`/`PermissionAuthorizationMiddleware`
como cualquier otro principal, así que la autorización existente funciona sin
cambios.

### 2.6 Integración con multitenancy

Un token o API key emitido para el tenant A no puede usarse contra el tenant B.
El claim de tenant se valida contra `ICurrentTenant` resuelto por
`TenantMiddleware`; discrepancia ⇒ 403. **Test obligatorio.**

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Features/ApiKeys/{ApiKey,ApiKeyErrors,IApiKeysRepository}.cs` + value objects
- `src/Raftel.Application/Features/ApiKeys/{CreateApiKey,RevokeApiKey,ListApiKeys}/**`
- `src/Raftel.Infrastructure/Authentication/ApiKeys/{ApiKeyAuthenticationHandler,ApiKeyHasher,ApiKeyGenerator}.cs`
- `src/Raftel.Infrastructure/Authentication/AuthenticationOptions.cs`, `CertificateOptions.cs`
- `src/Raftel.Infrastructure/Data/Configuration/ApiKeyConfiguration.cs`
- `tests/Raftel.Infrastructure.Tests/Authentication/ApiKeys/**`
- `tests/Raftel.Api.FunctionalTests/Authentication/**`

**Modificados**
- `src/Raftel.Infrastructure/DependencyInjection.cs` — `AddAuthentication`
- `src/Raftel.Api.Server/Features/Users/AuthorizationController.cs`
- `src/Raftel.Infrastructure/Authentication/{CurrentHttpUser,ClaimsPrincipalFactory,RaftelClaimTypes}.cs`
- `BREAKING_CHANGES.md`

## 4. Plan de implementación

1. Extraer `AuthenticationOptions` y hacer configurables los flujos actuales,
   manteniendo el comportamiento por defecto (paso sin cambio funcional).
2. **(test funcional)** Authorization code + PKCE completo: `/connect/authorize`
   ⇒ código ⇒ `/connect/token` ⇒ access + refresh.
3. **(test)** Sin `code_verifier` válido, el intercambio falla.
4. Habilitar authorization code y PKCE.
5. **(test)** Reutilizar un refresh token consumido revoca la cadena entera.
6. Activar rotación con detección de reutilización.
7. **(test)** En entorno Production sin certificado configurado, el arranque falla.
8. Implementar `CertificateOptions`.
9. Client credentials + tests.
10. **(test)** API key: el secreto solo se devuelve al crearla; el almacén guarda
    hash; una clave revocada o expirada ⇒ 401; comparación en tiempo constante.
11. Implementar el modelo, el handler de autenticación y los comandos de gestión.
12. **(test)** Una API key no puede tener permisos que su creador no tenga.
13. **(test)** Token/clave de tenant A contra tenant B ⇒ 403.
14. Cambiar el valor por defecto de password flow a `false` y documentar la
    migración.

## 5. Tests

- **Functional**: cada flujo de extremo a extremo.
- **Seguridad**: PKCE obligatorio, rotación con detección de reutilización, hash
  y comparación de API keys, escalada de privilegios, cruce de tenants.
- **Integration**: certificados, revocación, introspección.

## 6. Criterios de aceptación

- [ ] Authorization Code + PKCE disponible y con PKCE obligatorio.
- [ ] Password flow desactivado por defecto, con aviso al habilitarlo.
- [ ] El arranque falla en producción sin certificados configurados.
- [ ] Los secretos de API key nunca se almacenan ni se devuelven en claro tras su
      creación.
- [ ] Una API key no puede escalar privilegios.
- [ ] Ningún credencial cruza el límite de tenant.
- [ ] Rotación de refresh tokens con detección de reutilización.

## 7. Alternativas descartadas

- **Sustituir OpenIddict por IdentityServer/Duende**: Duende es de pago para uso
  comercial; OpenIddict es open source y ya está integrado.
- **JWT propio firmado a mano**: reimplementar criptografía y gestión de claves
  es exactamente lo que no hay que hacer.
- **API keys sin hash** (comparación directa): una filtración de la base de datos
  entrega todas las claves. Innegociable.
- **Mantener el password flow como principal**: desaconsejado por OAuth 2.1;
  se conserva como opción para escenarios legados y de confianza total.

## 8. Breaking changes

El password flow pasa a estar **desactivado por defecto**. Quien lo use debe
poner `EnablePasswordFlow = true` explícitamente. Se documenta con la
justificación y la ruta de migración a authorization code + PKCE.
