using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OpenScad;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the open OpenSCAD application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="catalog">Open openscad catalog service dependency used by the open OpenSCAD workflow to provide the corresponding application capability.</param>
/// <param name="documents">Open openscad document service dependency used by the open OpenSCAD workflow to provide the corresponding application capability.</param>
/// <param name="videoLayers">Open openscad video layer adapter dependency used by the open OpenSCAD workflow to provide the corresponding application capability.</param>
/// <param name="values">Open openscad value formatter dependency used by the open OpenSCAD workflow to provide the corresponding application capability.</param>
/// <param name="nodes">Open openscad node factory service dependency used by the open OpenSCAD workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/openscad")]
public sealed class OpenScadController(IOpenScadCatalogService catalog, IOpenScadDocumentService documents, IOpenScadVideoLayerAdapter videoLayers, IOpenScadValueFormatter values, IOpenScadNodeFactoryService nodes) : ControllerBase
{
    /// <summary>
    /// Returns the default node projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("nodes/{kind}/default")]
    public ActionResult<OpenScadNode> DefaultNode(string kind)
    {
        try { return Ok(nodes.Create(kind)); }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    /// <summary>
    /// Returns the catalog projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("catalog")]
    public ActionResult<IReadOnlyList<OpenScadNodeDefinition>> Catalog() => Ok(catalog.GetDefinitions());


    /// <summary>
    /// Returns the format value projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="value">Value value supplied to the open OpenSCAD operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("format-value")]
    public ActionResult<string> FormatValue([FromBody] OpenScadValue value) => Ok(values.Format(value));

    /// <summary>
    /// Returns the identifier projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="value">Value value supplied to the open OpenSCAD operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the open OpenSCAD operation and used when producing its result.</param>
    [HttpGet("identifier")]
    public ActionResult<string> Identifier([FromQuery] string value, [FromQuery] string fallback = "part") => Ok(values.Identifier(value, fallback));

    /// <summary>
    /// Returns the quote projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="value">Value value supplied to the open OpenSCAD operation and used when producing its result.</param>
    [HttpGet("quote")]
    public ActionResult<string> Quote([FromQuery] string value) => Ok(values.Quote(value));

    /// <summary>
    /// Returns the example projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("example")]
    public ActionResult<OpenScadDocument> Example() => Ok(documents.CreateExampleDocument());

    /// <summary>
    /// Returns the validate projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="document">Document value supplied to the open OpenSCAD operation and used when producing its result.</param>
    [HttpPost("validate")]
    public ActionResult<OpenScadValidationResult> Validate([FromBody] OpenScadDocument document) => Ok(documents.Validate(document));

    /// <summary>
    /// Returns the generate projection for the open OpenSCAD API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="document">Document value supplied to the open OpenSCAD operation and used when producing its result.</param>
    [HttpPost("generate")]
    public ActionResult<OpenScadGenerationResult> Generate([FromBody] OpenScadDocument document) => Ok(documents.Generate(document));

    /// <summary>
    /// Generates video layer for the open OpenSCAD API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="layer">Layer value supplied to the open OpenSCAD operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("video-layer")]
    public ActionResult<OpenScadGenerationResult> GenerateVideoLayer([FromBody] VideoEffectLayer layer) => Ok(new OpenScadGenerationResult
    {
        Script = videoLayers.CreateScript(layer), UsesAnimation = layer.AnimateMorph, RequiresNativeRender = true
    });
}
