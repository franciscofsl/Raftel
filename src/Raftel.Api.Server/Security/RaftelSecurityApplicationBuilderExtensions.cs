using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raftel.Api.Server.Middlewares;

namespace Raftel.Api.Server.Security;

/// <summary>
/// Pipeline half of <see cref="RaftelSecurityServiceCollectionExtensions.AddRaftelSecurity"/>. The order is:
/// <code>
/// app.UseCorrelationId();
/// app.UseRaftelExceptionHandling();
/// app.UseRaftelSecurity();        // forwarded headers, security headers, CORS
/// app.UseHttpsRedirection();
/// app.UseRouting();
/// app.UseAuthentication();
/// app.UseTenantMiddleware();
/// app.UseRaftelRateLimiting();    // after authentication and tenant resolution
/// app.UseAuthorization();
/// </code>
/// </summary>
public static class RaftelSecurityApplicationBuilderExtensions
{
    /// <summary>
    /// Adds forwarded headers, security headers and CORS. Register it early, before HTTPS redirection and
    /// authentication: forwarded headers must rewrite the client IP and scheme before anything reads them, and CORS
    /// must answer preflight requests before authentication rejects them.
    /// </summary>
    public static IApplicationBuilder UseRaftelSecurity(this IApplicationBuilder app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors();
        return app;
    }

    /// <summary>
    /// Adds the rate limiter. Register it after <c>UseRouting</c>, <c>UseAuthentication</c> and the tenant middleware,
    /// so endpoint policies are known and limits can be partitioned by user and tenant.
    /// Does nothing when <see cref="RaftelRateLimitOptions.Enabled"/> is <see langword="false"/>.
    /// </summary>
    public static IApplicationBuilder UseRaftelRateLimiting(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<RaftelSecurityOptions>>().Value;
        if (options.RateLimiting.Enabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }
}
