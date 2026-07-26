using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
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
    [HttpPost("default-blob")]
    public ActionResult<VideoEffectLayer> CreateDefaultBlob([FromQuery] string? name = null) => Ok(interchange.CreateDefaultBlobLayer(name));

    [HttpPost("openscad")]
    public ActionResult<string> OpenScad([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateOpenScad(layer));

    [HttpPost("mainframe")]
    public ActionResult<VideoLayerMainframeInsertRequest> Mainframe([FromBody] VideoEffectLayer layer) => Ok(interchange.CreateMainframeInsert(layer));

    [HttpGet("geometry/full-frame")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> FullFrame() => Ok(geometry.FullFrame());

    [HttpPost("geometry/normalize")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Normalize([FromBody] List<MediaFramePoint>? points) => Ok(geometry.Normalize(points));

    [HttpPost("geometry/resample")]
    public ActionResult<IReadOnlyList<MediaFramePoint>> Resample([FromBody] PolygonResampleRequest request) => Ok(geometry.Resample(request.Points, Math.Clamp(request.Count, 3, 128)));

    [HttpPost("geometry/openscad-points")]
    public ActionResult<string> OpenScadPoints([FromBody] List<MediaFramePoint>? points) => Ok(geometry.ToOpenScadPoints(geometry.Normalize(points)));

    [HttpPost("runtime/blob")]
    public ActionResult<string> BlobRuntime([FromBody] BrowserRuntimeTemplateRequest request) => Ok(browserRuntime.CreateBlobRuntime(request.Payload ?? "{}"));
}
