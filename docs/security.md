# Security: Rate Limiting, CORS and Security Headers

`Raftel.Api.Server` ships rate limiting, an explicit CORS policy, security
response headers and forwarded-header handling. Everything is built on
ASP.NET Core APIs (`Microsoft.AspNetCore.RateLimiting`, CORS, `HttpOverrides`),
so no extra NuGet package is required.

## Registration

```csharp
builder.Services.AddRaftelSecurity(builder.Configuration);

var app = builder.Build();

app.UseCorrelationId();
app.UseRaftelExceptionHandling();
app.UseRaftelSecurity();        // forwarded headers, security headers, CORS
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseRaftelRateLimiting();    // after authentication and tenant resolution
app.UseAuthorization();
```

The order matters:

- `UseRaftelSecurity` runs early. Forwarded headers must rewrite the client
  IP and scheme before anything reads them, and CORS must answer preflight
  requests before authentication rejects them. Register Swagger UI after it,
  so documentation responses also get the security headers.
- `UseRaftelRateLimiting` runs after `UseRouting`, `UseAuthentication` and
  the tenant middleware. Endpoint policies need routing, and the limits are
  partitioned by user and tenant.

Settings are bound from the `Raftel:Security` section, then from the optional
callback of `AddRaftelSecurity`. They are validated when the host starts.

## Rate limiting

| Policy | Algorithm | Default | Partition | Applied to |
|---|---|---|---|---|
| `raftel-global` | token bucket | 100 requests / minute | tenant + user, or IP | every endpoint |
| `raftel-auth` | fixed window | 5 attempts / 5 minutes | client IP | `/connect/token`, `POST /api/users/register` |
| `raftel-write` | sliding window | 30 requests / minute | tenant + user, or IP | every AutoEndpoints command |

- Anonymous callers are partitioned by client IP. Authenticated callers are
  partitioned by user, prefixed with the tenant, so a noisy tenant cannot use up
  another tenant's quota.
- Credential endpoints are partitioned by IP only. One client cannot stay under
  the limit by spreading attempts across many user names.
- A command can use another policy:
  `group.AddCommand<MyCommand>("route", HttpMethod.Post, rateLimitPolicy: "my-policy")`.
- Health endpoints are never rate limited.

A rejected request gets `429 Too Many Requests` with a `ProblemDetails` body
(`code`: `RateLimit.Exceeded`) and a `Retry-After` header. Its wide event is
emitted at `Warning` with `rate_limit.rejected = true`: repeated rejections on
credential endpoints signal a brute-force attempt.

```json
{
  "Raftel": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "Auth": { "PermitLimit": 5, "Window": "00:05:00" },
        "Write": { "PermitLimit": 30, "Window": "00:01:00" },
        "Global": { "PermitLimit": 100, "Window": "00:01:00" }
      }
    }
  }
}
```

Rate limiting complements ASP.NET Identity's account lockout. It does not
replace it: lockout alone lets anyone lock a known user out on purpose.

## Behind a reverse proxy

Behind a load balancer, the connection IP is the proxy's. Unless the proxy is
trusted, every client shares one rate limit partition: either the limit is
useless or it blocks everyone at once. List your proxies:

```json
{
  "Raftel": {
    "Security": {
      "ForwardedHeaders": {
        "KnownProxies": [ "10.0.0.10" ],
        "KnownNetworks": [ "10.0.0.0/8" ],
        "ForwardLimit": 1
      }
    }
  }
}
```

Only loopback and the listed addresses are trusted. `X-Forwarded-For` sent by
any other client is ignored, so a forged header cannot be used to evade the
limits. Do not trust every address: that brings the evasion back.

## CORS

No origin is allowed by default. An empty `AllowedOrigins` disables CORS: it
does not allow everything.

```json
{
  "Raftel": {
    "Security": {
      "Cors": {
        "AllowedOrigins": [ "https://app.example.com" ],
        "AllowCredentials": false,
        "PreflightMaxAge": "01:00:00"
      }
    }
  }
}
```

- Empty `AllowedMethods`, `AllowedHeaders` and `ExposedHeaders` use the
  framework defaults. The default exposed headers are `X-Correlation-Id` and
  `X-Total-Count`, so browsers can read the headers the framework emits.
- `AllowCredentials: true` with the `*` origin stops the host at startup.
  Browsers reject that combination, and allowing it anyway is a classic hole.
- `AllowDevelopmentOrigins: true` allows any origin in the Development
  environment only, and logs a warning. In any other environment it is ignored,
  also with a warning.

## Security headers

| Header | Default |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains`, only on HTTPS outside Development |
| `Server`, `X-Powered-By` | removed |

Requests under `DocumentationPath` (default `/swagger`) get
`DocumentationContentSecurityPolicy` instead, so Swagger UI can load its
scripts and styles. Set any header value to an empty string to omit that
header.
