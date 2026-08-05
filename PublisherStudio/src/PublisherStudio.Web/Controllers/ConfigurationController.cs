using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides configuration controller operations.
/// </summary>
[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController(IApplicationPathService paths, IFileLocalizationService localization) : ControllerBase
{
    /// <summary>
    /// Runs the paths operation.
    /// </summary>
    [HttpGet("paths")]
    public ActionResult<PublisherStudioPathOptions> Paths() => Ok(paths.GetDefaults());

    /// <summary>
    /// Resolves paths.
    /// </summary>
    [HttpPost("paths/resolve")]
    public ActionResult<PublisherStudioPathOptions> ResolvePaths([FromBody] PublisherStudioPathOptions overrides) => Ok(paths.Resolve(overrides));

    /// <summary>
    /// Ensures paths.
    /// </summary>
    [HttpPost("paths/ensure")]
    public IActionResult EnsurePaths([FromBody] PublisherStudioPathOptions? overrides = null)
    {
        paths.EnsureDirectories(overrides);
        return NoContent();
    }

    /// <summary>
    /// Runs the cultures operation.
    /// </summary>
    [HttpGet("localization/cultures")]
    public ActionResult<IReadOnlyList<string>> Cultures() => Ok(localization.GetAvailableCultures());

    /// <summary>
    /// Runs the strings operation.
    /// </summary>
    [HttpGet("localization/{culture}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Strings(string culture) => Ok(localization.GetStrings(culture));

    /// <summary>
    /// Saves strings.
    /// </summary>
    [HttpGet("localization/{culture}/{key}")]
    public ActionResult<string> String(string culture, string key) => Ok(localization.Get(key, culture));
    /// <summary>
    /// Saves strings.
    /// </summary>
    [HttpPut("localization/{culture}")]
    public async Task<IActionResult> SaveStrings(string culture, [FromBody] Dictionary<string, string> strings, CancellationToken cancellationToken)
    {
        await localization.SaveOverridesAsync(culture, strings, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Runs the select culture operation.
    /// </summary>
    [HttpGet("localization/select")]
    public IActionResult SelectCulture([FromQuery] string culture, [FromQuery] string? returnUrl = "/")
    {
        var available = localization.GetAvailableCultures();
        var selected = available.FirstOrDefault(item => string.Equals(item, culture, StringComparison.OrdinalIgnoreCase)) ?? "en-US";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(2), IsEssential = true, SameSite = SameSiteMode.Lax });
        var destination = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return LocalRedirect(destination);
    }

}

/// <summary>
/// Provides render export controller operations.
/// </summary>
[ApiController]
[Route("api/render-export")]
public sealed class RenderExportController(IRenderExportCatalogService exports) : ControllerBase
{
    /// <summary>
    /// Runs the capabilities operation.
    /// </summary>
    [HttpGet("capabilities")]
    public ActionResult<IReadOnlyList<RenderExportCapability>> Capabilities() => Ok(exports.GetCapabilities());

    /// <summary>
    /// Runs the capability operation.
    /// </summary>
    [HttpGet("capabilities/{format}")]
    public ActionResult<RenderExportCapability> Capability(string format) => exports.Find(format) is { } capability ? Ok(capability) : NotFound();
}
