using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
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
