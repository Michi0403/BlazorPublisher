using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.VideoStudio.Export;

namespace PublisherStudio.Controllers;

/// <summary>Loopback API for the same reusable Video Studio interchange services used by Mainframe and components.</summary>
[ApiController]
[Route("api/video-layer-interchange")]
public sealed class VideoLayerInterchangeController(
    IVideoLayerInterchangeService interchange,
    IPolygonGeometryService geometry,
    IBrowserRuntimeTemplateService browserRuntime) : ControllerBase
{
    /// <summary>
    /// Creates default blob.
    /// </summary>
    [HttpPost("default-blob")]
    public ActionResult<VideoEffectLayer> CreateDefaultBlob([FromQuery] string? name = null) => Ok(interchange.CreateDefaultBlobLayer(name));

    /// <summary>
    /// Opens scad.
    /// </summary>
    [HttpPost("openscad")]
    public ActionResult<string> OpenScad([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateOpenScad(layer));

    /// <summary>
    /// Runs the mainframe operation.
    /// </summary>
    [HttpPost("mainframe")]
    public ActionResult<VideoLayerMainframeInsertRequest> Mainframe([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateMainframeInsert(layer));

    /// <summary>
    /// Runs the full frame operation.
    /// </summary>
    [HttpGet("geometry/full-frame")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> FullFrame() => Ok(geometry.FullFrame());

    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
    [HttpPost("geometry/normalize")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Normalize([FromBody] List<MediaFramePoint>? points) => Ok(geometry.Normalize(points));

    /// <summary>
    /// Runs the resample operation.
    /// </summary>
    [HttpPost("geometry/resample")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Resample([FromBody] PolygonResampleRequest request) => Ok(geometry.Resample(request.Points, Math.Clamp(request.Count, 3, 128)));

    /// <summary>
    /// Opens scad points.
    /// </summary>
    [HttpPost("geometry/openscad-points")]
    public ActionResult<string> OpenScadPoints([FromBody] List<MediaFramePoint>? points) => Ok(geometry.ToOpenScadPoints(geometry.Normalize(points)));

    /// <summary>
    /// Runs the blob runtime operation.
    /// </summary>
    [HttpPost("runtime/blob")]
    public ActionResult<string> BlobRuntime([FromBody] BrowserRuntimeTemplateRequest request) => Ok(browserRuntime.CreateBlobRuntime(request.Payload ?? "{}"));
}
