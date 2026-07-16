using System.Globalization;

namespace PublisherStudio.Domain;

public readonly record struct PublicationPoint(double X, double Y);

public static class ConnectorGeometry
{
    public static bool TryResolve(PublicationPage page, ConnectorElement connector, out PublicationPoint source, out PublicationPoint target)
    {
        source = default;
        target = default;
        var sourceElement = page.Elements.FirstOrDefault(item => item.Id == connector.Source.ElementId && item is not ConnectorElement && item.Visible);
        var targetElement = page.Elements.FirstOrDefault(item => item.Id == connector.Target.ElementId && item is not ConnectorElement && item.Visible);
        if (sourceElement is null || targetElement is null) return false;
        source = Resolve(sourceElement, connector.Source.Anchor);
        target = Resolve(targetElement, connector.Target.Anchor);
        return true;
    }

    public static PublicationPoint Resolve(PublicationElement element, ConnectorAnchor anchor)
    {
        var local = anchor switch
        {
            ConnectorAnchor.TopLeft => new PublicationPoint(element.X, element.Y),
            ConnectorAnchor.Top => new PublicationPoint(element.X + element.Width / 2, element.Y),
            ConnectorAnchor.TopRight => new PublicationPoint(element.X + element.Width, element.Y),
            ConnectorAnchor.Right => new PublicationPoint(element.X + element.Width, element.Y + element.Height / 2),
            ConnectorAnchor.BottomRight => new PublicationPoint(element.X + element.Width, element.Y + element.Height),
            ConnectorAnchor.Bottom => new PublicationPoint(element.X + element.Width / 2, element.Y + element.Height),
            ConnectorAnchor.BottomLeft => new PublicationPoint(element.X, element.Y + element.Height),
            ConnectorAnchor.Left => new PublicationPoint(element.X, element.Y + element.Height / 2),
            _ => new PublicationPoint(element.X + element.Width / 2, element.Y + element.Height / 2)
        };

        if (Math.Abs(element.Rotation) < .001) return local;
        var centerX = element.X + element.Width / 2;
        var centerY = element.Y + element.Height / 2;
        var radians = element.Rotation * Math.PI / 180d;
        var dx = local.X - centerX;
        var dy = local.Y - centerY;
        return new PublicationPoint(
            centerX + dx * Math.Cos(radians) - dy * Math.Sin(radians),
            centerY + dx * Math.Sin(radians) + dy * Math.Cos(radians));
    }

    public static string Path(ConnectorElement connector, PublicationPoint source, PublicationPoint target)
    {
        return connector.PathKind switch
        {
            ConnectorPathKind.Elbow => ElbowPath(source, target),
            ConnectorPathKind.Curved => CurvedPath(connector, source, target),
            _ => $"M {Inv(source.X)} {Inv(source.Y)} L {Inv(target.X)} {Inv(target.Y)}"
        };
    }

    private static string ElbowPath(PublicationPoint source, PublicationPoint target)
    {
        var dx = Math.Abs(target.X - source.X);
        var dy = Math.Abs(target.Y - source.Y);
        if (dx >= dy)
        {
            var middle = (source.X + target.X) / 2;
            return $"M {Inv(source.X)} {Inv(source.Y)} L {Inv(middle)} {Inv(source.Y)} L {Inv(middle)} {Inv(target.Y)} L {Inv(target.X)} {Inv(target.Y)}";
        }
        else
        {
            var middle = (source.Y + target.Y) / 2;
            return $"M {Inv(source.X)} {Inv(source.Y)} L {Inv(source.X)} {Inv(middle)} L {Inv(target.X)} {Inv(middle)} L {Inv(target.X)} {Inv(target.Y)}";
        }
    }

    private static string CurvedPath(ConnectorElement connector, PublicationPoint source, PublicationPoint target)
    {
        var distance = Math.Max(16, Math.Min(70, Math.Sqrt(Math.Pow(target.X - source.X, 2) + Math.Pow(target.Y - source.Y, 2)) * .45));
        var c1 = ControlPoint(source, connector.Source.Anchor, distance);
        var c2 = ControlPoint(target, connector.Target.Anchor, distance);
        return $"M {Inv(source.X)} {Inv(source.Y)} C {Inv(c1.X)} {Inv(c1.Y)} {Inv(c2.X)} {Inv(c2.Y)} {Inv(target.X)} {Inv(target.Y)}";
    }

    private static PublicationPoint ControlPoint(PublicationPoint point, ConnectorAnchor anchor, double distance) => anchor switch
    {
        ConnectorAnchor.TopLeft or ConnectorAnchor.Top or ConnectorAnchor.TopRight => point with { Y = point.Y - distance },
        ConnectorAnchor.BottomLeft or ConnectorAnchor.Bottom or ConnectorAnchor.BottomRight => point with { Y = point.Y + distance },
        ConnectorAnchor.Left => point with { X = point.X - distance },
        ConnectorAnchor.Right => point with { X = point.X + distance },
        _ => point
    };

    public static string DashArray(ConnectorElement connector) => connector.DashStyle switch
    {
        ConnectorDashStyle.Dash => $"{Inv(connector.StrokeWidthMm * 5)} {Inv(connector.StrokeWidthMm * 3)}",
        ConnectorDashStyle.Dot => $"{Inv(connector.StrokeWidthMm)} {Inv(connector.StrokeWidthMm * 2.5)}",
        _ => string.Empty
    };

    private static string Inv(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
