using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController(IApplicationPathService paths, IFileLocalizationService localization) : ControllerBase
{
    [HttpGet("paths")]
    public ActionResult<PublisherStudioPathOptions> Paths() => Ok(paths.GetDefaults());

    [HttpPost("paths/resolve")]
    public ActionResult<PublisherStudioPathOptions> ResolvePaths([FromBody] PublisherStudioPathOptions overrides) => Ok(paths.Resolve(overrides));

    [HttpPost("paths/ensure")]
    public IActionResult EnsurePaths([FromBody] PublisherStudioPathOptions? overrides = null)
    {
        paths.EnsureDirectories(overrides);
        return NoContent();
    }

    [HttpGet("localization/cultures")]
    public ActionResult<IReadOnlyList<string>> Cultures() => Ok(localization.GetAvailableCultures());

    [HttpGet("localization/{culture}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Strings(string culture) => Ok(localization.GetStrings(culture));

    [HttpGet("localization/{culture}/{key}")]
    public ActionResult<string> String(string culture, string key) => Ok(localization.Get(key, culture));
    [HttpPut("localization/{culture}")]
    public async Task<IActionResult> SaveStrings(string culture, [FromBody] Dictionary<string, string> strings, CancellationToken cancellationToken)
    {
        await localization.SaveOverridesAsync(culture, strings, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

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

[ApiController]
[Route("api/render-export")]
public sealed class RenderExportController(IRenderExportCatalogService exports) : ControllerBase
{
    [HttpGet("capabilities")]
    public ActionResult<IReadOnlyList<RenderExportCapability>> Capabilities() => Ok(exports.GetCapabilities());

    [HttpGet("capabilities/{format}")]
    public ActionResult<RenderExportCapability> Capability(string format) => exports.Find(format) is { } capability ? Ok(capability) : NotFound();
}
