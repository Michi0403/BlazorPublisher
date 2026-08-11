using System.Text.Json;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OpenScad;

namespace PublisherStudio.Services.VideoStudio.Export;

/// <summary>
/// Defines the video layer interchange service contract.
/// </summary>
public interface IVideoLayerInterchangeService
{
    /// <summary>
    /// Creates default blob layer.
    /// </summary>
    VideoEffectLayer CreateDefaultBlobLayer(string? name = null);
    /// <summary>
    /// Creates open scad.
    /// </summary>
    string CreateOpenScad(VideoEffectLayer layer);
    /// <summary>
    /// Creates mainframe insert.
    /// </summary>
    VideoLayerMainframeInsertRequest CreateMainframeInsert(VideoEffectLayer layer);
}

/// <summary>
/// Coordinates portable browser and OpenSCAD representations from the canonical Video Studio layer model.
/// Geometry, OpenSCAD generation and browser-runtime templating are separate DI services so Mainframe,
/// controllers, plugins and future visual builders can reuse them without static coupling.
/// </summary>
public sealed class VideoLayerInterchangeService(
    IPolygonGeometryService geometry,
    IOpenScadVideoLayerAdapter openScad,
    IBrowserRuntimeTemplateService browserRuntime) : IVideoLayerInterchangeService
{
    /// <summary>
    /// Creates default blob layer.
    /// </summary>
    public VideoEffectLayer CreateDefaultBlobLayer(string? name = null)
    {
    try
    {
            var layer = new VideoEffectLayer
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Interactive 3D blob" : name.Trim(),
                Kind = VideoEffectLayerKind.Blob3D,
                MorphEnabled = true,
                AnimateMorph = true,
                MorphAmount = .5,
                AnimationSpeed = 1,
                Depth = .18,
                Roundness = .12,
                HtmlExportSupport = PublicationHtmlExportSupport.CanvasRuntime,
                HtmlExportNote = "Interactive and animated in Mainframe, Panel Studio and HTML export through the shared canvas runtime. Native OpenSCAD geometry must be rendered before static HTML output.",
                Region = new VideoFrameRegion
                {
                    Name = "Source region",
                    Points =
                    [
                        new() { X = .24, Y = .24 }, new() { X = .66, Y = .18 }, new() { X = .82, Y = .48 },
                        new() { X = .62, Y = .78 }, new() { X = .25, Y = .72 }, new() { X = .15, Y = .46 }
                    ]
                },
                MorphRegion = new VideoFrameRegion
                {
                    Name = "Morph target",
                    Points =
                    [
                        new() { X = .34, Y = .13 }, new() { X = .74, Y = .28 }, new() { X = .76, Y = .66 },
                        new() { X = .46, Y = .84 }, new() { X = .18, Y = .58 }, new() { X = .19, Y = .28 }
                    ]
                }
            };
            layer.OpenScadScript = CreateOpenScad(layer);
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method VideoLayerInterchangeService.CreateDefaultBlobLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Creates open scad.
    /// </summary>
    public string CreateOpenScad(VideoEffectLayer layer) {
    try
    {
        return openScad.CreateScript(layer);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method VideoLayerInterchangeService.CreateOpenScad failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Creates mainframe insert.
    /// </summary>
    public VideoLayerMainframeInsertRequest CreateMainframeInsert(VideoEffectLayer layer)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(layer);
            var source = geometry.Normalize(layer.Region?.Points);
            var target = geometry.Normalize(layer.MorphRegion?.Points);
            if (source.Count < 3) source = geometry.FullFrame();
            if (target.Count < 3) target = source.Select(geometry.Clone).ToList();
            var payload = JsonSerializer.Serialize(new
            {
                source = source.Select(point => new[] { point.X, point.Y }),
                target = target.Select(point => new[] { point.X, point.Y }),
                morphEnabled = layer.MorphEnabled && target.Count >= 3,
                animate = layer.AnimateMorph,
                morphAmount = Math.Clamp(layer.MorphAmount, 0, 1),
                speed = Math.Clamp(layer.AnimationSpeed, 0, 8),
                depth = Math.Clamp(layer.Depth, .02, .5),
                roundness = Math.Clamp(layer.Roundness, 0, .5),
                opacity = Math.Clamp(layer.Opacity, 0, 1)
            });
            return new VideoLayerMainframeInsertRequest
            {
                Name = string.IsNullOrWhiteSpace(layer.Name) ? "Interactive 3D object" : layer.Name.Trim(),
                Html = "<canvas class=\"publisher-3d-blob\" role=\"img\" aria-label=\"Animated 3D blob generated by Video Studio\"></canvas><div class=\"publisher-3d-badge\">HTML canvas · OpenSCAD interchange</div>",
                Css = "html,body{margin:0;width:100%;height:100%;overflow:hidden;background:transparent}.publisher-3d-blob{display:block;width:100%;height:100%}.publisher-3d-badge{position:absolute;left:8px;bottom:8px;padding:3px 6px;border-radius:4px;background:rgba(2,6,23,.7);color:#dbeafe;font:11px Segoe UI,system-ui,sans-serif;pointer-events:none}",
                JavaScript = browserRuntime.CreateBlobRuntime(payload),
                OpenScadScript = CreateOpenScad(layer),
                HtmlExportSupport = PublicationHtmlExportSupport.CanvasRuntime,
                HtmlExportNote = "Interactive and animated in Mainframe, Panel Studio and HTML export through the shared canvas runtime. Native OpenSCAD geometry must be rendered before static HTML output."
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method VideoLayerInterchangeService.CreateMainframeInsert failed: {__serviceMethodException}");
        throw;
    }
}
}
