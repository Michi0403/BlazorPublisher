namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported publication layer move values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationLayerMove
{
    BringToFront,
    BringForward,
    SendBackward,
    SendToBack
}

/// <summary>
/// Represents a publication canvas bounds application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationCanvasBounds
{
    /// <summary>
    /// Gets or sets the x value that forms part of the publication canvas bounds state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="PublicationCanvasBounds"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the publication canvas bounds state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="PublicationCanvasBounds"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the publication canvas bounds state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationCanvasBounds"/>.</value>
    public double Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the publication canvas bounds state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PublicationCanvasBounds"/>.</value>
    public double Height { get; set; }
}

/// <summary>
/// Represents the input contract for publication layout constraint, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class PublicationLayoutConstraintRequest
{
    /// <summary>
    /// Gets or sets the canvas width value that forms part of the publication layout constraint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas width value exposed by <see cref="PublicationLayoutConstraintRequest"/>.</value>
    public double CanvasWidth { get; set; } = 160;
    /// <summary>
    /// Gets or sets the canvas height value that forms part of the publication layout constraint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas height value exposed by <see cref="PublicationLayoutConstraintRequest"/>.</value>
    public double CanvasHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets the bounds value that forms part of the publication layout constraint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The bounds value exposed by <see cref="PublicationLayoutConstraintRequest"/>.</value>
    public PublicationCanvasBounds Bounds { get; set; } = new();
}

/// <summary>
/// Represents a publication layer item application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationLayerItem
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication layer item instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationLayerItem"/>.</value>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the z index value that forms part of the publication layer item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The z index value exposed by <see cref="PublicationLayerItem"/>.</value>
    public int ZIndex { get; set; }
}

/// <summary>
/// Represents the input contract for publication layer order, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class PublicationLayerOrderRequest
{
    /// <summary>
    /// Gets or sets the stable element identifier used to identify or correlate this publication layer order instance with related application state.
    /// </summary>
    /// <value>The element identifier value exposed by <see cref="PublicationLayerOrderRequest"/>.</value>
    public Guid ElementId { get; set; }
    /// <summary>
    /// Gets or sets the move value that forms part of the publication layer order state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The move value exposed by <see cref="PublicationLayerOrderRequest"/>.</value>
    public PublicationLayerMove Move { get; set; }
    /// <summary>
    /// Gets or sets the elements collection maintained or exposed by this publication layer order instance for downstream processing.
    /// </summary>
    /// <value>The elements value exposed by <see cref="PublicationLayerOrderRequest"/>.</value>
    public List<PublicationLayerItem> Elements { get; set; } = [];
}
