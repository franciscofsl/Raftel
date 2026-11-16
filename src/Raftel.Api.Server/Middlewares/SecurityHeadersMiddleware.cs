using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raftel.Api.Server.Security;

namespace Raftel.Api.Server.Middlewares;

/// <summary>
/// Adds the security response headers from <see cref="SecurityHeadersOptions"/> and removes <c>Server</c> and
/// <c>X-Powered-By</c>. Headers are written when the response starts, so they also cover responses produced by
/// middlewares registered before this one, such as the exception handler.
/// </summary>
internal sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IOptions<RaftelSecurityOptions> options,
    IHostEnvironment environment)
{
    private readonly SecurityHeadersOptions _headers = options.Value.Headers;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            Apply(context);
            return Task.CompletedTask;
        });

        return next(context);
    }

    private void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;

        SetIfConfigured(headers, "X-Content-Type-Options", _headers.ContentTypeOptions);
        SetIfConfigured(headers, "X-Frame-Options", _headers.FrameOptions);
        SetIfConfigured(headers, "Referrer-Policy", _headers.ReferrerPolicy);
        SetIfConfigured(headers, "Content-Security-Policy", ContentSecurityPolicyFor(context.Request.Path));

        if (context.Request.IsHttps && !environment.IsDevelopment())
        {
            SetIfConfigured(headers, "Strict-Transport-Security", _headers.StrictTransportSecurity);
        }

        headers.Remove("Server");
        headers.Remove("X-Powered-By");
    }

    private string ContentSecurityPolicyFor(PathString path)
    {
        var isDocumentation = !string.IsNullOrEmpty(_headers.DocumentationPath)
                              && path.StartsWithSegments(_headers.DocumentationPath);

        return isDocumentation ? _headers.DocumentationContentSecurityPolicy : _headers.ContentSecurityPolicy;
    }

    private static void SetIfConfigured(IHeaderDictionary headers, string name, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            headers[name] = value;
        }
    }
}
