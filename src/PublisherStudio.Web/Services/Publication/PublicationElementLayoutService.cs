using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Publication;

/// <summary>
/// Defines the publication element layout service contract.
/// </summary>
public interface IPublicationElementLayoutService
{
    PublicationCanvasBounds Constrain(PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight);
    void ApplyBounds(PublicationElement element, PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight);
    void Nudge(PublicationElement element, double deltaX, double deltaY, double canvasWidth, double canvasHeight);
    int NextZ(IEnumerable<PublicationElement> elements);
    void NormalizeZOrder(IList<PublicationElement> elements);
    bool MoveLayer(IList<PublicationElement> elements, Guid elementId, PublicationLayerMove move);
    IReadOnlyList<PublicationLayerItem> Reorder(IReadOnlyList<PublicationLayerItem> elements, Guid elementId, PublicationLayerMove move);
}

/// <summary>
/// Shared layout policy for Mainframe, Panel Studio, API calls and future media-suite object layers.
/// It keeps geometry and z-order rules out of UI components so pointer, keyboard, touch, controller
/// and automation inputs all commit through the same deterministic behavior.
/// </summary>
public sealed class PublicationElementLayoutService(ILogger<PublicationElementLayoutService> logger) : IPublicationElementLayoutService
{
    /// <summary>
    /// Runs the constrain operation.
    /// </summary>
    public PublicationCanvasBounds Constrain(PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(bounds);
            var widthLimit = Math.Max(1, canvasWidth);
            var heightLimit = Math.Max(1, canvasHeight);
            var width = Math.Clamp(Safe(bounds.Width, 1), 1, widthLimit);
            var height = Math.Clamp(Safe(bounds.Height, 1), 1, heightLimit);
            return new PublicationCanvasBounds
            {
                X = Math.Clamp(Safe(bounds.X), 0, Math.Max(0, widthLimit - width)),
                Y = Math.Clamp(Safe(bounds.Y), 0, Math.Max(0, heightLimit - height)),
                Width = width,
                Height = height
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Constrain)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Constrain)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies bounds.
    /// </summary>
    public void ApplyBounds(PublicationElement element, PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(element);
            var constrained = Constrain(bounds, canvasWidth, canvasHeight);
            element.X = constrained.X;
            element.Y = constrained.Y;
            element.Width = constrained.Width;
            element.Height = constrained.Height;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(ApplyBounds)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(ApplyBounds)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the nudge operation.
    /// </summary>
    public void Nudge(PublicationElement element, double deltaX, double deltaY, double canvasWidth, double canvasHeight)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(element);
            ApplyBounds(element, new PublicationCanvasBounds
            {
                X = element.X + Safe(deltaX),
                Y = element.Y + Safe(deltaY),
                Width = element.Width,
                Height = element.Height
            }, canvasWidth, canvasHeight);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Nudge)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Nudge)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the next z operation.
    /// </summary>
    public int NextZ(IEnumerable<PublicationElement> elements)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(elements);
            return elements.Select(element => element.ZIndex).DefaultIfEmpty(0).Max() + 1;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(NextZ)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(NextZ)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes zorder.
    /// </summary>
    public void NormalizeZOrder(IList<PublicationElement> elements)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(elements);
            var ordered = elements
                .Select((element, index) => new { Element = element, Index = index })
                .OrderBy(item => item.Element.ZIndex)
                .ThenBy(item => item.Index)
                .Select(item => item.Element)
                .ToList();
            for (var index = 0; index < ordered.Count; index++) ordered[index].ZIndex = index + 1;
            if (elements is List<PublicationElement> list)
            {
                list.Clear();
                list.AddRange(ordered);
            }
            else
            {
                for (var index = 0; index < ordered.Count; index++) elements[index] = ordered[index];
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(NormalizeZOrder)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(NormalizeZOrder)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the move layer operation.
    /// </summary>
    public bool MoveLayer(IList<PublicationElement> elements, Guid elementId, PublicationLayerMove move)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(elements);
            var ordered = elements
                .Select((element, index) => new { Element = element, Index = index })
                .OrderBy(item => item.Element.ZIndex)
                .ThenBy(item => item.Index)
                .Select(item => item.Element)
                .ToList();
            var index = ordered.FindIndex(element => element.Id == elementId);
            if (index < 0) return false;
            var target = move switch
            {
                PublicationLayerMove.BringToFront => ordered.Count - 1,
                PublicationLayerMove.BringForward => Math.Min(ordered.Count - 1, index + 1),
                PublicationLayerMove.SendBackward => Math.Max(0, index - 1),
                PublicationLayerMove.SendToBack => 0,
                _ => index
            };
            if (target == index)
            {
                NormalizeZOrder(elements);
                return false;
            }
            var selected = ordered[index];
            ordered.RemoveAt(index);
            ordered.Insert(target, selected);
            for (var position = 0; position < ordered.Count; position++) ordered[position].ZIndex = position + 1;
            elements.Clear();
            foreach (var element in ordered) elements.Add(element);
            logger.LogDebug("Moved publication element {ElementId} using {Move} to z-index {ZIndex}.", elementId, move, selected.ZIndex);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(MoveLayer)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(MoveLayer)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the reorder operation.
    /// </summary>
    public IReadOnlyList<PublicationLayerItem> Reorder(IReadOnlyList<PublicationLayerItem> elements, Guid elementId, PublicationLayerMove move)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(elements);
            var ordered = elements
                .Select((element, index) => new { Element = new PublicationLayerItem { Id = element.Id, ZIndex = element.ZIndex }, Index = index })
                .OrderBy(item => item.Element.ZIndex)
                .ThenBy(item => item.Index)
                .Select(item => item.Element)
                .ToList();
            var index = ordered.FindIndex(element => element.Id == elementId);
            if (index < 0) return ordered.AsReadOnly();
            var target = move switch
            {
                PublicationLayerMove.BringToFront => ordered.Count - 1,
                PublicationLayerMove.BringForward => Math.Min(ordered.Count - 1, index + 1),
                PublicationLayerMove.SendBackward => Math.Max(0, index - 1),
                PublicationLayerMove.SendToBack => 0,
                _ => index
            };
            var selected = ordered[index];
            ordered.RemoveAt(index);
            ordered.Insert(target, selected);
            for (var position = 0; position < ordered.Count; position++) ordered[position].ZIndex = position + 1;
            return ordered.AsReadOnly();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Reorder)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Reorder)} failed.");
        throw;
    }
}

    private double Safe(double value, double fallback = 0) {
    try
    {
        return double.IsFinite(value) ? value : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Safe)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(Safe)} failed.");
        throw;
    }
}
}
