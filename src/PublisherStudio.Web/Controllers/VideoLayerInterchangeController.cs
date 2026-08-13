using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.VideoStudio.Export;

namespace PublisherStudio.Controllers;

/// <summary>Loopback API for the same reusable Video Studio interchange services used by Mainframe and components.</summary>
/// <param name="interchange">Video layer interchange service dependency used by the video layer interchange workflow to provide the corresponding application capability.</param>
/// <param name="geometry">Polygon geometry service dependency used by the video layer interchange workflow to provide the corresponding application capability.</param>
/// <param name="browserRuntime">Browser runtime template service dependency used by the video layer interchange workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/video-layer-interchange")]
public sealed class VideoLayerInterchangeController(
    IVideoLayerInterchangeService interchange,
    IPolygonGeometryService geometry,
    IBrowserRuntimeTemplateService browserRuntime) : ControllerBase
{
    /// <summary>
    /// Creates default blob for the video layer interchange API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="name">Name value supplied to the video layer interchange operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("default-blob")]
    public ActionResult<VideoEffectLayer> CreateDefaultBlob([FromQuery] string? name = null) => Ok(interchange.CreateDefaultBlobLayer(name));

    /// <summary>
    /// Opens OpenSCAD for the video layer interchange API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="layer">Layer value supplied to the video layer interchange operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("openscad")]
    public ActionResult<string> OpenScad([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateOpenScad(layer));

    /// <summary>
    /// Returns the mainframe projection for the video layer interchange API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="layer">Layer value supplied to the video layer interchange operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("mainframe")]
    public ActionResult<VideoLayerMainframeInsertRequest> Mainframe([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateMainframeInsert(layer));

    /// <summary>
    /// Returns the full frame projection for the video layer interchange API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("geometry/full-frame")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> FullFrame() => Ok(geometry.FullFrame());

    /// <summary>
    /// Returns the normalize projection for the video layer interchange API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="points">Points value supplied to the video layer interchange operation and used when producing its result.</param>
    [HttpPost("geometry/normalize")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Normalize([FromBody] List<MediaFramePoint>? points) => Ok(geometry.Normalize(points));

    /// <summary>
    /// Returns the resample projection for the video layer interchange API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPost("geometry/resample")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Resample([FromBody] PolygonResampleRequest request) => Ok(geometry.Resample(request.Points, Math.Clamp(request.Count, 3, 128)));

    /// <summary>
    /// Opens OpenSCAD points for the video layer interchange API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="points">Points value supplied to the video layer interchange operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("geometry/openscad-points")]
    public ActionResult<string> OpenScadPoints([FromBody] List<MediaFramePoint>? points) => Ok(geometry.ToOpenScadPoints(geometry.Normalize(points)));

    /// <summary>
    /// Returns the blob runtime projection for the video layer interchange API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("runtime/blob")]
    public ActionResult<string> BlobRuntime([FromBody] BrowserRuntimeTemplateRequest request) => Ok(browserRuntime.CreateBlobRuntime(request.Payload ?? "{}"));
}
