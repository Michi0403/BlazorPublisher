using System.Globalization;

namespace PublisherStudio.Domain;

public readonly record struct PublicationPoint(double X, double Y);

public static class ConnectorGeometry
{
    public static bool TryResolve(PublicationPage page, ConnectorElement connector, out PublicationPoint source, out PublicationPoint target)
    {
        if (!TryResolveEndpoint(page, connector.Source, out source))
        {
            target = default;
            return false;
        }

        return TryResolveEndpoint(page, connector.Target, out target);
    }

    public static bool TryResolveEndpoint(PublicationPage page, ConnectorEndpoint endpoint, out PublicationPoint point)
    {
        if (endpoint.Kind == ConnectorEndpointKind.Canvas)
        {
            point = new PublicationPoint(
                Math.Clamp(endpoint.X, 0, Math.Max(0, page.WidthMm)),
                Math.Clamp(endpoint.Y, 0, Math.Max(0, page.HeightMm)));
            return true;
        }

        var element = page.Elements.FirstOrDefault(item =>
            item.Id == endpoint.ElementId &&
            item is not ConnectorElement &&
            item.Visible);
        if (element is null)
        {
            point = default;
            return false;
        }

        if (endpoint.PortId is { } portId)
        {
            var port = element.ConnectorPorts.FirstOrDefault(candidate => candidate.Id == portId);
            if (port is not null)
            {
                point = Resolve(element, port.XPercent, port.YPercent);
                return true;
            }
        }

        point = Resolve(element, endpoint.Anchor);
        return true;
    }

    public static PublicationPoint Resolve(PublicationElement element, ConnectorAnchor anchor)
    {
        var relative = anchor switch
        {
            ConnectorAnchor.TopLeft => new PublicationPoint(0, 0),
            ConnectorAnchor.Top => new PublicationPoint(.5, 0),
            ConnectorAnchor.TopRight => new PublicationPoint(1, 0),
            ConnectorAnchor.Right => new PublicationPoint(1, .5),
            ConnectorAnchor.BottomRight => new PublicationPoint(1, 1),
            ConnectorAnchor.Bottom => new PublicationPoint(.5, 1),
            ConnectorAnchor.BottomLeft => new PublicationPoint(0, 1),
            ConnectorAnchor.Left => new PublicationPoint(0, .5),
            _ => new PublicationPoint(.5, .5)
        };
        return Resolve(element, relative.X, relative.Y);
    }

    public static PublicationPoint Resolve(PublicationElement element, double xPercent, double yPercent)
    {
        var rawX = element.X + element.Width * Math.Clamp(xPercent, 0, 1);
        var rawY = element.Y + element.Height * Math.Clamp(yPercent, 0, 1);
        if (Math.Abs(element.Rotation) < .001) return new PublicationPoint(rawX, rawY);

        var centerX = element.X + element.Width / 2;
        var centerY = element.Y + element.Height / 2;
        var radians = element.Rotation * Math.PI / 180d;
        var dx = rawX - centerX;
        var dy = rawY - centerY;
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

    public static (PublicationPoint First, PublicationPoint Second) ControlPoints(
        ConnectorElement connector,
        PublicationPoint source,
        PublicationPoint target)
    {
        if (connector.Control1X is { } c1x && connector.Control1Y is { } c1y &&
            connector.Control2X is { } c2x && connector.Control2Y is { } c2y &&
            double.IsFinite(c1x) && double.IsFinite(c1y) && double.IsFinite(c2x) && double.IsFinite(c2y))
        {
            return (new PublicationPoint(c1x, c1y), new PublicationPoint(c2x, c2y));
        }

        var distance = Math.Max(16, Math.Min(70, Math.Sqrt(Math.Pow(target.X - source.X, 2) + Math.Pow(target.Y - source.Y, 2)) * .45));
        return (ControlPoint(source, connector.Source.Anchor, distance), ControlPoint(target, connector.Target.Anchor, distance));
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

        var verticalMiddle = (source.Y + target.Y) / 2;
        return $"M {Inv(source.X)} {Inv(source.Y)} L {Inv(source.X)} {Inv(verticalMiddle)} L {Inv(target.X)} {Inv(verticalMiddle)} L {Inv(target.X)} {Inv(target.Y)}";
    }

    private static string CurvedPath(ConnectorElement connector, PublicationPoint source, PublicationPoint target)
    {
        var controls = ControlPoints(connector, source, target);
        return $"M {Inv(source.X)} {Inv(source.Y)} C {Inv(controls.First.X)} {Inv(controls.First.Y)} {Inv(controls.Second.X)} {Inv(controls.Second.Y)} {Inv(target.X)} {Inv(target.Y)}";
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
