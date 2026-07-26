using System.Globalization;
using System.Text;
using System.Text.Json;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.VideoStudio.Export;

/// <summary>
/// Creates portable browser and OpenSCAD representations from the canonical Video Studio layer model.
/// The browser representation is used by Mainframe/Panel/HTML export; the OpenSCAD script is an
/// interchange artifact and is explicitly marked as requiring a native render before it can become
/// a pixel-perfect HTML video effect.
/// </summary>
public sealed class VideoLayerInterchangeService
{
    public VideoEffectLayer CreateDefaultBlobLayer(string? name = null)
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
            HtmlExportNote = "Interactive and animated in Mainframe, Panel Studio and HTML export through the shared canvas runtime. Native OpenSCAD mesh rendering must be baked before export.",
            Region = new VideoFrameRegion
            {
                Name = "Source region",
                Points =
                [
                    new() { X = .24, Y = .24 },
                    new() { X = .66, Y = .18 },
                    new() { X = .82, Y = .48 },
                    new() { X = .62, Y = .78 },
                    new() { X = .25, Y = .72 },
                    new() { X = .15, Y = .46 }
                ]
            },
            MorphRegion = new VideoFrameRegion
            {
                Name = "Morph target",
                Points =
                [
                    new() { X = .34, Y = .13 },
                    new() { X = .74, Y = .28 },
                    new() { X = .76, Y = .66 },
                    new() { X = .46, Y = .84 },
                    new() { X = .18, Y = .58 },
                    new() { X = .19, Y = .28 }
                ]
            }
        };
        layer.OpenScadScript = CreateOpenScad(layer);
        return layer;
    }

    public string CreateOpenScad(VideoEffectLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var source = NormalizePoints(layer.Region?.Points);
        var target = NormalizePoints(layer.MorphRegion?.Points);
        if (source.Count < 3) source = FullFrame();
        if (target.Count < 3) target = source.Select(ClonePoint).ToList();
        var pointCount = Math.Clamp(Math.Max(source.Count, target.Count), 3, 128);
        source = ResamplePolygon(source, pointCount);
        target = ResamplePolygon(target, pointCount);

        var depth = Math.Clamp(double.IsFinite(layer.Depth) ? layer.Depth : .18, .02, 1) * 100;
        var rounding = Math.Min(depth * .45, Math.Clamp(double.IsFinite(layer.Roundness) ? layer.Roundness : .12, 0, .5) * 40);
        var sb = new StringBuilder();
        sb.AppendLine("// PublisherStudio Video Studio / OpenMorph-compatible OpenSCAD interchange");
        sb.AppendLine("// Browser HTML uses the canvas fallback. Render this script to STL/PNG/video for native OpenSCAD output.");
        sb.AppendLine("$fn = 48;");
        sb.AppendLine($"depth = {Inv(depth)};");
        sb.AppendLine($"rounding = {Inv(rounding)};");
        sb.AppendLine("morph = 0.5; // 0 = selected region, 1 = morph target");
        sb.AppendLine($"source_points = {ScadPoints(source)};");
        sb.AppendLine($"target_points = {ScadPoints(target)};");
        sb.AppendLine();
        sb.AppendLine("function lerp(a,b,t) = a + (b-a)*t;");
        sb.AppendLine("function morph_points(a,b,t) = [for(i=[0:min(len(a),len(b))-1]) [lerp(a[i][0],b[i][0],t), lerp(a[i][1],b[i][1],t)]];");
        sb.AppendLine();
        sb.AppendLine("module publisher_blob(t=morph) {");
        sb.AppendLine("    pts = morph_points(source_points, target_points, t);");
        sb.AppendLine("    if (rounding > 0)");
        sb.AppendLine("        minkowski() {");
        sb.AppendLine("            linear_extrude(height=max(0.1, depth-2*rounding), center=true) polygon(points=pts);");
        sb.AppendLine("            sphere(r=rounding);");
        sb.AppendLine("        }");
        sb.AppendLine("    else");
        sb.AppendLine("        linear_extrude(height=depth, center=true) polygon(points=pts);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("publisher_blob();");
        return sb.ToString();
    }

    public VideoLayerMainframeInsertRequest CreateMainframeInsert(VideoEffectLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var source = NormalizePoints(layer.Region?.Points);
        var target = NormalizePoints(layer.MorphRegion?.Points);
        if (source.Count < 3) source = FullFrame();
        if (target.Count < 3) target = source.Select(ClonePoint).ToList();

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
            Name = string.IsNullOrWhiteSpace(layer.Name) ? "3D video object" : $"{layer.Name} · 3D object",
            Html = "<canvas class=\"publisher-3d-blob\" role=\"img\" aria-label=\"Animated 3D blob generated by Video Studio\"></canvas><div class=\"publisher-3d-badge\">HTML canvas · OpenSCAD interchange</div>",
            Css = "html,body{margin:0;width:100%;height:100%;overflow:hidden;background:transparent}.publisher-3d-blob{display:block;width:100%;height:100%}.publisher-3d-badge{position:absolute;left:8px;bottom:8px;padding:3px 6px;border-radius:4px;background:rgba(2,6,23,.7);color:#dbeafe;font:11px Segoe UI,system-ui,sans-serif;pointer-events:none}",
            JavaScript = BrowserRuntime(payload),
            OpenScadScript = CreateOpenScad(layer),
            HtmlExportSupport = PublicationHtmlExportSupport.CanvasRuntime,
            HtmlExportNote = "Interactive and animated in Mainframe, Panel Studio and HTML export through the shared canvas runtime. Native OpenSCAD mesh rendering must be baked before export."
        };
    }

    private static string BrowserRuntime(string payload) => $$"""
(() => {
  const config = {{payload}};
  const canvas = document.querySelector('.publisher-3d-blob');
  if (!(canvas instanceof HTMLCanvasElement)) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const clamp = (v,a,b) => Math.max(a,Math.min(b,Number.isFinite(Number(v))?Number(v):a));
  const resample = (points,count) => {
    if (!Array.isArray(points) || points.length < 2) return [];
    const lengths=[]; let total=0;
    for(let i=0;i<points.length;i++){
      const a=points[i], b=points[(i+1)%points.length];
      const len=Math.hypot(b[0]-a[0],b[1]-a[1]); lengths.push(len); total+=len;
    }
    if(total<=1e-6) return Array.from({length:count},()=>points[0].slice());
    const result=[];
    for(let i=0;i<count;i++){
      let distance=total*i/count, edge=0;
      while(edge<lengths.length-1 && distance>lengths[edge]){distance-=lengths[edge];edge++;}
      const a=points[edge], b=points[(edge+1)%points.length], t=lengths[edge]>0?distance/lengths[edge]:0;
      result.push([a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t]);
    }
    return result;
  };
  const count=Math.max(3,Math.min(128,Math.max(config.source.length,config.target.length)));
  const source=resample(config.source,count), target=resample(config.target,count);
  const resize=()=>{
    const ratio=Math.min(2,Math.max(1,window.devicePixelRatio||1));
    const w=Math.max(2,Math.round(canvas.clientWidth*ratio)),h=Math.max(2,Math.round(canvas.clientHeight*ratio));
    if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}
  };
  const path=(points,offsetX=0,offsetY=0)=>{
    ctx.beginPath();
    points.forEach((point,index)=>{
      const x=(.08+.84*point[0])*canvas.width+offsetX;
      const y=(.08+.84*point[1])*canvas.height+offsetY;
      if(index===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
    });
    ctx.closePath();
  };
  const draw=time=>{
    resize(); ctx.clearRect(0,0,canvas.width,canvas.height);
    const phase=config.animate&&config.morphEnabled ? (Math.sin(time*.001*config.speed*Math.PI)+1)/2 : clamp(config.morphAmount,0,1);
    const points=source.map((point,index)=>[point[0]+(target[index][0]-point[0])*phase,point[1]+(target[index][1]-point[1])*phase]);
    const depth=Math.max(2,Math.round(Math.min(canvas.width,canvas.height)*config.depth*.22));
    for(let step=depth;step>0;step--){
      path(points,step*.62,step*.78); ctx.fillStyle=`rgba(2,20,42,${.18+.48*(1-step/depth)})`;ctx.fill();
    }
    path(points);
    const gradient=ctx.createLinearGradient(0,0,canvas.width,canvas.height);
    gradient.addColorStop(0,'rgba(125,211,252,.98)');gradient.addColorStop(.48,'rgba(14,165,233,.92)');gradient.addColorStop(1,'rgba(30,64,175,.96)');
    ctx.globalAlpha=clamp(config.opacity,0,1);ctx.fillStyle=gradient;ctx.fill();
    ctx.save();ctx.clip();
    const shine=ctx.createRadialGradient(canvas.width*.32,canvas.height*.25,0,canvas.width*.32,canvas.height*.25,Math.max(canvas.width,canvas.height)*.7);
    shine.addColorStop(0,'rgba(255,255,255,.62)');shine.addColorStop(.35,'rgba(255,255,255,.08)');shine.addColorStop(1,'rgba(0,0,0,.34)');
    ctx.fillStyle=shine;ctx.fillRect(0,0,canvas.width,canvas.height);ctx.restore();ctx.globalAlpha=1;
    requestAnimationFrame(draw);
  };
  requestAnimationFrame(draw);
})();
""";

    private static List<MediaFramePoint> NormalizePoints(IEnumerable<MediaFramePoint>? points) => (points ?? [])
        .Where(point => point is not null)
        .Take(128)
        .Select(point => new MediaFramePoint
        {
            X = Math.Clamp(double.IsFinite(point.X) ? point.X : 0, 0, 1),
            Y = Math.Clamp(double.IsFinite(point.Y) ? point.Y : 0, 0, 1)
        })
        .ToList();

    private static List<MediaFramePoint> FullFrame() =>
    [
        new() { X = .12, Y = .12 }, new() { X = .88, Y = .12 },
        new() { X = .88, Y = .88 }, new() { X = .12, Y = .88 }
    ];

    private static List<MediaFramePoint> ResamplePolygon(IReadOnlyList<MediaFramePoint> points, int count)
    {
        if (points.Count < 3 || count < 3) return [];
        var lengths = new double[points.Count];
        var total = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            var length = Math.Hypot(next.X - current.X, next.Y - current.Y);
            lengths[index] = length;
            total += length;
        }

        if (total <= 1e-8)
            return Enumerable.Range(0, count).Select(_ => ClonePoint(points[0])).ToList();

        var result = new List<MediaFramePoint>(count);
        for (var sample = 0; sample < count; sample++)
        {
            var distance = total * sample / count;
            var edge = 0;
            while (edge < lengths.Length - 1 && distance > lengths[edge])
            {
                distance -= lengths[edge];
                edge++;
            }

            var current = points[edge];
            var next = points[(edge + 1) % points.Count];
            var amount = lengths[edge] > 1e-8 ? distance / lengths[edge] : 0;
            result.Add(new MediaFramePoint
            {
                X = current.X + (next.X - current.X) * amount,
                Y = current.Y + (next.Y - current.Y) * amount
            });
        }
        return result;
    }

    private static MediaFramePoint ClonePoint(MediaFramePoint point) => new() { X = point.X, Y = point.Y };
    private static string ScadPoints(IEnumerable<MediaFramePoint> points) =>
        $"[{string.Join(", ", points.Select(point => $"[{Inv(point.X * 100)}, {Inv((1 - point.Y) * 100)}]"))}]";
    private static string Inv(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
