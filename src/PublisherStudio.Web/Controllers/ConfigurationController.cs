using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the configuration application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="paths">Application path service dependency used by the configuration workflow to provide the corresponding application capability.</param>
/// <param name="localization">File localization service dependency used by the configuration workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController(
    IApplicationPathService paths,
    IFileLocalizationService localization,
    ILogger<ConfigurationController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the paths projection for the configuration API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("paths")]
    public ActionResult<PublisherStudioPathOptions> Paths() => Ok(paths.GetDefaults());

    /// <summary>
    /// Resolves paths for the configuration API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="overrides">Overrides value supplied to the configuration operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("paths/resolve")]
    public ActionResult<PublisherStudioPathOptions> ResolvePaths([FromBody] PublisherStudioPathOptions overrides) => Ok(paths.Resolve(overrides));

    /// <summary>
    /// Ensures paths for the configuration API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="overrides">Overrides value supplied to the configuration operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("paths/ensure")]
    public IActionResult EnsurePaths([FromBody] PublisherStudioPathOptions? overrides = null)
    {
        paths.EnsureDirectories(overrides);
        return NoContent();
    }

    /// <summary>
    /// Returns the cultures projection for the configuration API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization/cultures")]
    public ActionResult<IReadOnlyList<string>> Cultures() => Ok(localization.GetAvailableCultures());

    /// <summary>
    /// Returns the strings projection for the configuration API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="culture">Culture value supplied to the configuration operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization/{culture}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Strings(string culture) => Ok(localization.GetStrings(culture));

    /// <summary>
    /// Returns the string projection for the configuration API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="culture">Culture value supplied to the configuration operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the configuration operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization/{culture}/{key}")]
    public ActionResult<string> String(string culture, string key) => Ok(localization.Get(key, culture));
    /// <summary>
    /// Persists strings for the configuration API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="culture">Culture value supplied to the configuration operation and used when producing its result.</param>
    /// <param name="strings">Strings value supplied to the configuration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPut("localization/{culture}")]
    public async Task<IActionResult> SaveStrings(string culture, [FromBody] Dictionary<string, string> strings, CancellationToken cancellationToken)
    {
        await localization.SaveOverridesAsync(culture, strings, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Returns the select culture projection for the configuration API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="culture">Culture value supplied to the configuration operation and used when producing its result.</param>
    /// <param name="returnUrl">Return url value supplied to the configuration operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("localization/select")]
    public IActionResult SelectCulture([FromQuery] string culture, [FromQuery] string? returnUrl = "/")
    {
        var selected = localization.ResolveAvailableCulture(culture);
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = "/",
                MaxAge = TimeSpan.FromDays(365),
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        var localReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl;
        var redirectUrl = localization.BuildCultureRedirectUrl(localReturnUrl, selected);
        logger.LogInformation(
            "PublisherStudio UI culture changed to {Culture}; reloading {ReturnUrl} with an explicit request culture.",
            selected,
            redirectUrl);
        return LocalRedirect(redirectUrl);
    }

}

/// <summary>
/// Exposes the render export application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="exports">Render export catalog service dependency used by the render export workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/render-export")]
public sealed class RenderExportController(IRenderExportCatalogService exports) : ControllerBase
{
    /// <summary>
    /// Returns the capabilities projection for the render export API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capabilities")]
    public ActionResult<IReadOnlyList<RenderExportCapability>> Capabilities() => Ok(exports.GetCapabilities());

    /// <summary>
    /// Returns the capability projection for the render export API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="format">Format value supplied to the render export operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capabilities/{format}")]
    public ActionResult<RenderExportCapability> Capability(string format) => exports.Find(format) is { } capability ? Ok(capability) : NotFound();
}
