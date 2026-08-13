using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Publication;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the publication layout application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="layout">Publication element layout service dependency used by the publication layout workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/publication/layout")]
public sealed class PublicationLayoutController(IPublicationElementLayoutService layout) : ControllerBase
{
    /// <summary>
    /// Returns the constrain projection for the publication layout API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("constrain")]
    public ActionResult<PublicationCanvasBounds> Constrain([FromBody] PublicationLayoutConstraintRequest request) =>
        Ok(layout.Constrain(request.Bounds, request.CanvasWidth, request.CanvasHeight));

    /// <summary>
    /// Returns the reorder projection for the publication layout API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("reorder")]
    public ActionResult<IReadOnlyList<PublicationLayerItem>> Reorder([FromBody] PublicationLayerOrderRequest request) =>
        Ok(layout.Reorder(request.Elements, request.ElementId, request.Move));
}
