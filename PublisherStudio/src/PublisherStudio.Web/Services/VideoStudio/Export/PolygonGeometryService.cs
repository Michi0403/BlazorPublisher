using System.Globalization;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.VideoStudio.Export;

public interface IPolygonGeometryService
{
    List<MediaFramePoint> Normalize(IEnumerable<MediaFramePoint>? points);
    List<MediaFramePoint> FullFrame();
    List<MediaFramePoint> Resample(IReadOnlyList<MediaFramePoint> points, int count);
    MediaFramePoint Clone(MediaFramePoint point);
    double Distance(double deltaX, double deltaY);
    string ToOpenScadPoints(IEnumerable<MediaFramePoint> points);
    string Number(double value);
}

public sealed class PolygonGeometryService : IPolygonGeometryService
{
    public List<MediaFramePoint> Normalize(IEnumerable<MediaFramePoint>? points) =>
    [
        .. (points ?? [])
            .Where(point => point is not null)
            .Take(128)
            .Select(point => new MediaFramePoint
            {
                X = Math.Clamp(double.IsFinite(point.X) ? point.X : 0, 0, 1),
                Y = Math.Clamp(double.IsFinite(point.Y) ? point.Y : 0, 0, 1)
            })
    ];

    public List<MediaFramePoint> FullFrame() =>
    [
        new() { X = .12, Y = .12 }, new() { X = .88, Y = .12 },
        new() { X = .88, Y = .88 }, new() { X = .12, Y = .88 }
    ];

    public List<MediaFramePoint> Resample(IReadOnlyList<MediaFramePoint> points, int count)
    {
        if (points.Count < 3 || count < 3) return [];
        var lengths = new double[points.Count];
        var total = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            lengths[index] = Distance(next.X - current.X, next.Y - current.Y);
            total += lengths[index];
        }
        if (total <= 1e-8) return [.. Enumerable.Range(0, count).Select(_ => Clone(points[0]))];
        var result = new List<MediaFramePoint>(count);
        for (var sample = 0; sample < count; sample++)
        {
            var distance = total * sample / count;
            var edge = 0;
            while (edge < lengths.Length - 1 && distance > lengths[edge]) { distance -= lengths[edge]; edge++; }
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

    public MediaFramePoint Clone(MediaFramePoint point) => new() { X = point.X, Y = point.Y };

    public double Distance(double deltaX, double deltaY)
    {
        var x = Math.Abs(deltaX); var y = Math.Abs(deltaY);
        if (x < y) (x, y) = (y, x);
        if (x <= double.Epsilon) return 0;
        var ratio = y / x;
        return x * Math.Sqrt(1 + ratio * ratio);
    }

    public string ToOpenScadPoints(IEnumerable<MediaFramePoint> points) =>
        $"[{string.Join(", ", points.Select(point => $"[{Number(point.X * 100)}, {Number((1 - point.Y) * 100)}]"))}]";

    public string Number(double value) => (double.IsFinite(value) ? value : 0).ToString("0.######", CultureInfo.InvariantCulture);
}
