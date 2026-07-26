namespace PublisherStudio.Domain;

public enum PublicationLayerMove
{
    BringToFront,
    BringForward,
    SendBackward,
    SendToBack
}

public sealed class PublicationCanvasBounds
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public sealed class PublicationLayoutConstraintRequest
{
    public double CanvasWidth { get; set; } = 160;
    public double CanvasHeight { get; set; } = 90;
    public PublicationCanvasBounds Bounds { get; set; } = new();
}

public sealed class PublicationLayerItem
{
    public Guid Id { get; set; }
    public int ZIndex { get; set; }
}

public sealed class PublicationLayerOrderRequest
{
    public Guid ElementId { get; set; }
    public PublicationLayerMove Move { get; set; }
    public List<PublicationLayerItem> Elements { get; set; } = [];
}
