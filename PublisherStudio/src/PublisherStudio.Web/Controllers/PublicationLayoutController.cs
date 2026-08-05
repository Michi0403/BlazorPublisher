using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Publication;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides publication layout controller operations.
/// </summary>
[ApiController]
[Route("api/publication/layout")]
public sealed class PublicationLayoutController(IPublicationElementLayoutService layout) : ControllerBase
{
    /// <summary>
    /// Runs the constrain operation.
    /// </summary>
    [HttpPost("constrain")]
    public ActionResult<PublicationCanvasBounds> Constrain([FromBody] PublicationLayoutConstraintRequest request) =>
        Ok(layout.Constrain(request.Bounds, request.CanvasWidth, request.CanvasHeight));

    /// <summary>
    /// Runs the reorder operation.
    /// </summary>
    [HttpPost("reorder")]
    public ActionResult<IReadOnlyList<PublicationLayerItem>> Reorder([FromBody] PublicationLayerOrderRequest request) =>
        Ok(layout.Reorder(request.Elements, request.ElementId, request.Move));
}
