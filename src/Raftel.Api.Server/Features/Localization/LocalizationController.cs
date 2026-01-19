using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raftel.Application.Localization;

namespace Raftel.Api.Server.Features.Localization;

/// <summary>
/// Controller for managing localization resources.
/// </summary>
[ApiController]
[Route("api/localization")]
public class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _localizationService;

    public LocalizationController(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Gets localization resources for the specified culture and optional modules.
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en", "es").</param>
    /// <param name="modules">Comma-separated list of module names (optional).</param>
    /// <returns>The localization resource containing all translations.</returns>
    [HttpGet("resources")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResources([FromQuery] string? cultureName = null, [FromQuery] string? modules = null)
    {
        var culture = cultureName ?? System.Globalization.CultureInfo.CurrentUICulture.Name;
        var moduleNames = string.IsNullOrEmpty(modules)
            ? null
            : modules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var resource = await _localizationService.GetResourcesAsync(culture, moduleNames);

        return Ok(resource);
    }

    /// <summary>
    /// Gets localization resources for the specified culture.
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en", "es").</param>
    /// <returns>The localization resource containing all translations.</returns>
    [HttpGet("resources/{cultureName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResourcesByCulture(string cultureName)
    {
        var resource = await _localizationService.GetResourcesAsync(cultureName);
        return Ok(resource);
    }
}
