using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Publication;

/// <summary>
/// Defines the contract for publication element layout behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublicationElementLayoutService
{
    /// <summary>
    /// Performs constrain as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bounds">Bounds value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>The publication canvas bounds produced by the operation.</returns>
    PublicationCanvasBounds Constrain(PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight);
    /// <summary>
    /// Applies bounds as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="bounds">Bounds value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
    void ApplyBounds(PublicationElement element, PublicationCanvasBounds bounds, double canvasWidth, double canvasHeight);
    /// <summary>
    /// Finds a visible placement for a newly inserted element while minimizing overlap with existing panel content.
    /// </summary>
    /// <param name="elements">Existing elements that already occupy the target canvas.</param>
    /// <param name="width">Requested width of the new element.</param>
    /// <param name="height">Requested height of the new element.</param>
    /// <param name="canvasWidth">Width of the target canvas.</param>
    /// <param name="canvasHeight">Height of the target canvas.</param>
    /// <returns>Constrained bounds that prefer the canvas center and then the nearest low-overlap position.</returns>
    PublicationCanvasBounds FindAvailablePlacement(IEnumerable<PublicationElement> elements, double width, double height, double canvasWidth, double canvasHeight);
    /// <summary>
    /// Performs nudge as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="deltaX">Delta x value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="deltaY">Delta y value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
    void Nudge(PublicationElement element, double deltaX, double deltaY, double canvasWidth, double canvasHeight);
    /// <summary>
    /// Performs next z as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <returns>The int produced by the operation.</returns>
    int NextZ(IEnumerable<PublicationElement> elements);
    /// <summary>
    /// Normalizes z order as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    void NormalizeZOrder(IList<PublicationElement> elements);
    /// <summary>
    /// Performs move layer as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="move">Move value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool MoveLayer(IList<PublicationElement> elements, Guid elementId, PublicationLayerMove move);
    /// <summary>
    /// Performs reorder as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication layer item dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="move">Move value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<PublicationLayerItem> Reorder(IReadOnlyList<PublicationLayerItem> elements, Guid elementId, PublicationLayerMove move);
}

/// <summary>
/// Shared layout policy for Mainframe, Panel Studio, API calls and future media-suite object layers.
/// It keeps geometry and z-order rules out of UI components so pointer, keyboard, touch, controller
/// and automation inputs all commit through the same deterministic behavior.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationElementLayoutService(ILogger<PublicationElementLayoutService> logger) : IPublicationElementLayoutService
{
    /// <summary>
    /// Performs constrain as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bounds">Bounds value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>The publication canvas bounds produced by the operation.</returns>
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
    /// Applies bounds as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="bounds">Bounds value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
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
    /// Finds a visible placement for a newly inserted element while minimizing overlap with existing panel content.
    /// </summary>
    /// <param name="elements">Existing elements that already occupy the target canvas.</param>
    /// <param name="width">Requested width of the new element.</param>
    /// <param name="height">Requested height of the new element.</param>
    /// <param name="canvasWidth">Width of the target canvas.</param>
    /// <param name="canvasHeight">Height of the target canvas.</param>
    /// <returns>Constrained bounds that prefer the canvas center and then the nearest low-overlap position.</returns>
    public PublicationCanvasBounds FindAvailablePlacement(IEnumerable<PublicationElement> elements, double width, double height, double canvasWidth, double canvasHeight)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(elements);
            var widthLimit = Math.Max(1, canvasWidth);
            var heightLimit = Math.Max(1, canvasHeight);
            var requested = Constrain(new PublicationCanvasBounds
            {
                Width = width,
                Height = height
            }, widthLimit, heightLimit);
            var maxX = Math.Max(0, widthLimit - requested.Width);
            var maxY = Math.Max(0, heightLimit - requested.Height);
            var centerX = maxX / 2;
            var centerY = maxY / 2;
            var existing = elements.Where(element => element.Visible).ToArray();
            if (existing.Length == 0)
            {
                requested.X = centerX;
                requested.Y = centerY;
                return requested;
            }

            var stepX = Math.Max(4, Math.Min(16, requested.Width * .18));
            var stepY = Math.Max(4, Math.Min(12, requested.Height * .22));
            var candidates = new List<PublicationCanvasBounds>
            {
                new() { X = centerX, Y = centerY, Width = requested.Width, Height = requested.Height }
            };
            for (var y = 0d; y <= maxY + .001; y += stepY)
            {
                for (var x = 0d; x <= maxX + .001; x += stepX)
                {
                    candidates.Add(new PublicationCanvasBounds
                    {
                        X = Math.Min(x, maxX),
                        Y = Math.Min(y, maxY),
                        Width = requested.Width,
                        Height = requested.Height
                    });
                }
            }
            candidates.Add(new PublicationCanvasBounds { X = maxX, Y = maxY, Width = requested.Width, Height = requested.Height });

            PublicationCanvasBounds? best = null;
            var bestScore = double.MaxValue;
            foreach (var candidate in candidates
                .OrderBy(candidate => Math.Pow(candidate.X - centerX, 2) + Math.Pow(candidate.Y - centerY, 2)))
            {
                var overlap = 0d;
                foreach (var occupied in existing)
                {
                    var left = Math.Max(candidate.X, occupied.X);
                    var top = Math.Max(candidate.Y, occupied.Y);
                    var right = Math.Min(candidate.X + candidate.Width, occupied.X + occupied.Width);
                    var bottom = Math.Min(candidate.Y + candidate.Height, occupied.Y + occupied.Height);
                    if (right > left && bottom > top)
                        overlap += (right - left) * (bottom - top);
                }

                if (overlap <= .001)
                    return Constrain(candidate, widthLimit, heightLimit);

                var centerDistance = Math.Pow(candidate.X - centerX, 2) + Math.Pow(candidate.Y - centerY, 2);
                var score = overlap + centerDistance * .0005;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return Constrain(best ?? requested, widthLimit, heightLimit);

    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(FindAvailablePlacement)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementLayoutService)}.{nameof(FindAvailablePlacement)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs nudge as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="deltaX">Delta x value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="deltaY">Delta y value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasWidth">Canvas width value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="canvasHeight">Canvas height value supplied to the publication element layout operation and used when producing its result.</param>
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
    /// Performs next z as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <returns>The int produced by the operation.</returns>
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
    /// Normalizes z order as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
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
    /// Performs move layer as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="move">Move value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs reorder as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elements">Publication layer item dependency used by the publication element layout workflow to provide the corresponding application capability.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="move">Move value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs safe as part of the publication element layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication element layout operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the publication element layout operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
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
