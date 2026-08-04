using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Publication;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/publication/layout")]
public sealed class PublicationLayoutController(IPublicationElementLayoutService layout) : ControllerBase
{
    [HttpPost("constrain")]
    public ActionResult<PublicationCanvasBounds> Constrain([FromBody] PublicationLayoutConstraintRequest request) =>
        Ok(layout.Constrain(request.Bounds, request.CanvasWidth, request.CanvasHeight));

    [HttpPost("reorder")]
    public ActionResult<IReadOnlyList<PublicationLayerItem>> Reorder([FromBody] PublicationLayerOrderRequest request) =>
        Ok(layout.Reorder(request.Elements, request.ElementId, request.Move));
}
