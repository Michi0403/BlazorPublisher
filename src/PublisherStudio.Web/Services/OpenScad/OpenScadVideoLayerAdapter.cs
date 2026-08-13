using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.VideoStudio.Export;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Represents an open OpenSCAD video layer adapter application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="geometry">Polygon geometry service dependency used by the open OpenSCAD video layer adapter workflow to provide the corresponding application capability.</param>
/// <param name="documents">Open openscad document service dependency used by the open OpenSCAD video layer adapter workflow to provide the corresponding application capability.</param>
public sealed class OpenScadVideoLayerAdapter(
    IPolygonGeometryService geometry,
    IOpenScadDocumentService documents) : IOpenScadVideoLayerAdapter
{
    /// <summary>
    /// Creates script for <see cref="OpenScadVideoLayerAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD video layer adapter workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the open OpenSCAD video layer adapter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateScript(VideoEffectLayer layer)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(layer);
            var source = geometry.Normalize(layer.Region?.Points);
            var target = geometry.Normalize(layer.MorphRegion?.Points);
            if (source.Count < 3) source = geometry.FullFrame();
            if (target.Count < 3) target = source.Select(geometry.Clone).ToList();
            var pointCount = Math.Clamp(Math.Max(source.Count, target.Count), 3, 128);
            source = geometry.Resample(source, pointCount);
            target = geometry.Resample(target, pointCount);
            var depth = Math.Clamp(double.IsFinite(layer.Depth) ? layer.Depth : .18, .02, 1) * 100;
            var rounding = Math.Min(depth * .45, Math.Clamp(double.IsFinite(layer.Roundness) ? layer.Roundness : .12, 0, .5) * 40);

            var raw = new OpenScadNode
            {
                Name = "OpenMorph-compatible morphing blob", Kind = "raw",
                Parameters = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["code"] = new OpenScadValue
                    {
                        Type = OpenScadParameterType.Expression,
                        Text = $$"""
    // OpenMorph-compatible selection blob. $t animates the assembled source and target regions.
    depth = {{geometry.Number(depth)}};
    rounding = {{geometry.Number(rounding)}};
    source_points = {{geometry.ToOpenScadPoints(source)}};
    target_points = {{geometry.ToOpenScadPoints(target)}};
    function ps_blob_lerp(a,b,t)=a+(b-a)*t;
    function ps_blob_points(a,b,t)=[for(i=[0:min(len(a),len(b))-1]) [ps_blob_lerp(a[i][0],b[i][0],t),ps_blob_lerp(a[i][1],b[i][1],t)]];
    module publisher_blob(t=$t) {
        pts=ps_blob_points(source_points,target_points,t);
        if(rounding>0)
            minkowski() {
                linear_extrude(height=max(0.1,depth-2*rounding),center=true) polygon(points=pts);
                sphere(r=rounding);
            }
        else
            linear_extrude(height=depth,center=true) polygon(points=pts);
    }
    publisher_blob();
    """
                    }
                }
            };
            return documents.Generate(new OpenScadDocument
            {
                Name = string.IsNullOrWhiteSpace(layer.Name) ? "Video selection blob" : layer.Name,
                Facets = 48,
                Roots = [raw],
                Metadata = new(StringComparer.OrdinalIgnoreCase) { ["source"] = "VideoStudio" }
            }).Script;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadVideoLayerAdapter.CreateScript failed: {__serviceMethodException}");
        throw;
    }
}
}
