using PublisherStudio.BusinessObjects;
using System.Globalization;

// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a connector geometry application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ConnectorGeometry(ILogger<ConnectorGeometry> logger)
{
    /// <summary>
    /// Attempts to resolve for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="page">Page value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="connector">Connector value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryResolve(PublicationPage page, ConnectorElement connector, out PublicationPoint source, out PublicationPoint target)
    {
    try
    {
            logger.LogTrace("Resolving connector geometry for page {PageId}.", page.Id);
            if (!TryResolveEndpoint(page, connector.Source, out source))
            {
                target = default;
                return false;
            }

            return TryResolveEndpoint(page, connector.Target, out target);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(TryResolve)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(TryResolve)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to resolve endpoint for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="page">Page value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="point">Point value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryResolveEndpoint(PublicationPage page, ConnectorEndpoint endpoint, out PublicationPoint point)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(TryResolveEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(TryResolveEndpoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs resolve for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="anchor">Anchor value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The publication point produced by the operation.</returns>
    public PublicationPoint Resolve(PublicationElement element, ConnectorAnchor anchor)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Resolve)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Resolve)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs resolve for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="xPercent">X percent value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="yPercent">Y percent value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The publication point produced by the operation.</returns>
    public PublicationPoint Resolve(PublicationElement element, double xPercent, double yPercent)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Resolve)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Resolve)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs path for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="connector">Connector value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Path(ConnectorElement connector, PublicationPoint source, PublicationPoint target)
    {
    try
    {
            return connector.PathKind switch
            {
                ConnectorPathKind.Elbow => ElbowPath(source, target),
                ConnectorPathKind.Curved => CurvedPath(connector, source, target),
                _ => $"M {Inv(source.X)} {Inv(source.Y)} L {Inv(target.X)} {Inv(target.Y)}"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Path)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Path)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs control points for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="connector">Connector value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The publication point first publication point second produced by the operation.</returns>
    public (PublicationPoint First, PublicationPoint Second) ControlPoints(
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

    /// <summary>
    /// Performs elbow path for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ElbowPath(PublicationPoint source, PublicationPoint target)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(ElbowPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(ElbowPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs curved path for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="connector">Connector value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CurvedPath(ConnectorElement connector, PublicationPoint source, PublicationPoint target)
    {
    try
    {
            var controls = ControlPoints(connector, source, target);
            return $"M {Inv(source.X)} {Inv(source.Y)} C {Inv(controls.First.X)} {Inv(controls.First.Y)} {Inv(controls.Second.X)} {Inv(controls.Second.Y)} {Inv(target.X)} {Inv(target.Y)}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(CurvedPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(CurvedPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs control point for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="point">Point value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="anchor">Anchor value supplied to the connector geometry operation and used when producing its result.</param>
    /// <param name="distance">Distance value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The publication point produced by the operation.</returns>
    private PublicationPoint ControlPoint(PublicationPoint point, ConnectorAnchor anchor, double distance) {
    try
    {
        return anchor switch
    {
        ConnectorAnchor.TopLeft or ConnectorAnchor.Top or ConnectorAnchor.TopRight => point with { Y = point.Y - distance },
        ConnectorAnchor.BottomLeft or ConnectorAnchor.Bottom or ConnectorAnchor.BottomRight => point with { Y = point.Y + distance },
        ConnectorAnchor.Left => point with { X = point.X - distance },
        ConnectorAnchor.Right => point with { X = point.X + distance },
        _ => point
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(ControlPoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(ControlPoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs dash array for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="connector">Connector value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string DashArray(ConnectorElement connector) {
    try
    {
        return connector.DashStyle switch
    {
        ConnectorDashStyle.Dash => $"{Inv(connector.StrokeWidthMm * 5)} {Inv(connector.StrokeWidthMm * 3)}",
        ConnectorDashStyle.Dot => $"{Inv(connector.StrokeWidthMm)} {Inv(connector.StrokeWidthMm * 2.5)}",
        _ => string.Empty
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(DashArray)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(DashArray)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs inv for <see cref="ConnectorGeometry"/>, keeping the operation consistent with the state and invariants of the surrounding connector geometry workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the connector geometry operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Inv(double value) {
    try
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Inv)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ConnectorGeometry)}.{nameof(Inv)} failed.");
        throw;
    }
}
}
