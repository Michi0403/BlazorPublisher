namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported publication layer move values.
/// </summary>
public enum PublicationLayerMove
{
    BringToFront,
    BringForward,
    SendBackward,
    SendToBack
}

/// <summary>
/// Represents a publication canvas bounds.
/// </summary>
public sealed class PublicationCanvasBounds
{
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public double Width { get; set; }
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// Represents a publication layout constraint request.
/// </summary>
public sealed class PublicationLayoutConstraintRequest
{
    /// <summary>
    /// Gets or sets canvas width.
    /// </summary>
    public double CanvasWidth { get; set; } = 160;
    /// <summary>
    /// Gets or sets canvas height.
    /// </summary>
    public double CanvasHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets bounds.
    /// </summary>
    public PublicationCanvasBounds Bounds { get; set; } = new();
}

/// <summary>
/// Represents a publication layer item.
/// </summary>
public sealed class PublicationLayerItem
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets zindex.
    /// </summary>
    public int ZIndex { get; set; }
}

/// <summary>
/// Represents a publication layer order request.
/// </summary>
public sealed class PublicationLayerOrderRequest
{
    /// <summary>
    /// Gets or sets element identifier.
    /// </summary>
    public Guid ElementId { get; set; }
    /// <summary>
    /// Gets or sets move.
    /// </summary>
    public PublicationLayerMove Move { get; set; }
    /// <summary>
    /// Gets or sets elements.
    /// </summary>
    public List<PublicationLayerItem> Elements { get; set; } = [];
}
