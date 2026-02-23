using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Raftel.Application.Localization;

namespace Raftel.Api.Server.Features.Localization;

/// <summary>
/// Middleware for detecting and setting the current culture based on HTTP request.
/// </summary>
public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LocalizationOptions _options;

    public LocalizationMiddleware(RequestDelegate next, IOptions<LocalizationOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var culture = GetCultureFromRequest(context);

        if (!string.IsNullOrEmpty(culture) && _options.SupportedCultures.Contains(culture))
        {
            var cultureInfo = new CultureInfo(culture);
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
        }
        else
        {
            var defaultCultureInfo = new CultureInfo(_options.DefaultCulture);
            CultureInfo.CurrentCulture = defaultCultureInfo;
            CultureInfo.CurrentUICulture = defaultCultureInfo;
        }

        await _next(context);
    }

    private string? GetCultureFromRequest(HttpContext context)
    {
        // 1. Check query parameter
        if (context.Request.Query.TryGetValue("culture", out var cultureQuery))
        {
            return cultureQuery.ToString();
        }

        // 2. Check cookie
        if (context.Request.Cookies.TryGetValue("culture", out var cultureCookie))
        {
            return cultureCookie;
        }

        // 3. Check Accept-Language header
        var acceptLanguage = context.Request.Headers.AcceptLanguage.FirstOrDefault();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var languages = acceptLanguage.Split(',');
            if (languages.Length > 0)
            {
                var primaryLanguage = languages[0].Split(';')[0].Trim();
                
                // Try exact match first
                if (_options.SupportedCultures.Contains(primaryLanguage))
                {
                    return primaryLanguage;
                }

                // Try neutral culture (e.g., "es" from "es-MX")
                var neutralCulture = primaryLanguage.Split('-')[0];
                if (_options.SupportedCultures.Contains(neutralCulture))
                {
                    return neutralCulture;
                }
            }
        }

        return _options.DefaultCulture;
    }
}
